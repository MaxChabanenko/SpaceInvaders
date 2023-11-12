using lab1_SpaceInvaders.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace lab1_SpaceInvaders
{
    internal static class Program
    {
        static int FrameCount = 0;
        static double SpawnRate = 50;
        static int sleepTime = 30;

        static int EscapedEnemyCount = 0;
        static int GameScore = 0;

        static readonly char EnemyChar = '#';
        static readonly char heroChar = '@';
        static readonly char bulletChar = '^';

        static List<Entity> Enemies = new List<Entity>();
        static Entity Hero = new Entity(38, 30, heroChar);

        static Mutex mutex = new Mutex();
        static Semaphore sem = new Semaphore(3, 3);
        static ManualResetEvent userInputEvent = new ManualResetEvent(false);

        private static void ReadKey()
        {
            try
            {
                var keyInfo = Console.ReadKey(true); //pressed
                if(keyInfo.Key == ConsoleKey.A|| keyInfo.Key == ConsoleKey.D)
                    userInputEvent.Set();
            }
            catch { }
        }

        private static void TimerCallback(object state)//15 seconds over
        {
            userInputEvent.Set(); 
        }

        static void Main(string[] args)
        {
            Console.SetWindowSize(80, 35);

            Console.SetCursorPosition(10, 25);
            Console.Write("A and D to move, Space to shoot. Move or wait 15 sec to start");

            Timer timer = new Timer(TimerCallback, null, 15000, Timeout.Infinite);

            Thread inputThread = new Thread(ReadKey);
            inputThread.Start();
            userInputEvent.WaitOne();
            inputThread.Interrupt();
            timer.Dispose();



            Thread t = new Thread(EnemiesManager);
            t.Start();
            Console.SetCursorPosition(10, 25);
            Console.Write(new string(' ', Console.WindowWidth));

            Console.SetCursorPosition(0, 31);
            Console.Write(new string('_', Console.WindowWidth));

            try
            {
                while (true)
                {
                    mutex.WaitOne();
                    UnRenderEntity(Hero);
                    Console.SetCursorPosition(50, 0);
                    Console.Write(String.Format("Miss: {0}", EscapedEnemyCount));
                    Console.SetCursorPosition(5, 0);
                    Console.Write(String.Format("Hit: {0}", GameScore));
                    mutex.ReleaseMutex();

                    if (EscapedEnemyCount == 30)
                    {
                        Console.SetCursorPosition(25, 15);
                        Console.Write("You lost! Try again");
                        Thread.CurrentThread.Interrupt();
                    }
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.A:
                                Hero.PositionX -= 1;
                                break;
                            case ConsoleKey.D:
                                Hero.PositionX += 1;
                                break;

                            case ConsoleKey.Spacebar:
                                Thread b = new Thread(Bullet);
                                b.Start();
                                break;
                        }
                    }
                    mutex.WaitOne();
                    RenderHero(Hero);
                    mutex.ReleaseMutex();

                    FrameCount++;
                    Thread.Sleep(sleepTime);
                }
            }
            catch { }
        }

        static void Bullet()
        {
            try
            {
                sem.WaitOne();
                Entity bullet = new Entity(Hero.PositionX, Hero.PositionY, bulletChar);
                while (true)
                {
                    mutex.WaitOne();
                    UnRenderEntity(bullet);
                    mutex.ReleaseMutex();


                    if (!CheckKill(bullet))
                    {
                        if (bullet.PositionY != 1)
                        {
                            bullet.PositionY -= 1;
                        }

                    }
                    else
                        Thread.CurrentThread.Interrupt();

                    mutex.WaitOne();
                    RenderBullet(bullet);
                    mutex.ReleaseMutex();

                    Thread.Sleep(sleepTime);
                }
            }
            catch
            {
                sem.Release(); 
            }
        }

        static void EnemiesManager()
        {
            Random random = new Random();

            while (true)
            {
                mutex.WaitOne();
                UnRenderEnemies(Enemies);
                mutex.ReleaseMutex();

                if (FrameCount % (int)(SpawnRate / 4) == 0)
                {
                    UpdateEnemyLocation();
                }
                if (FrameCount % SpawnRate == 0)
                {
                    int x = random.Next(10, 70);
                    var Enemy = new Entity(x, 1, EnemyChar);
                    Enemies.Add(Enemy);
                }
                mutex.WaitOne();
                RenderEnemies(Enemies);
                mutex.ReleaseMutex();

                if (FrameCount % 60 == 0 && SpawnRate > 10)
                {
                    SpawnRate -= 2;
                }

                Thread.Sleep(sleepTime);
            }
        }

        static bool CheckKill(Entity bullet)
        {
            if (bullet != null)
            {
                var killedEnemy = Enemies.Find(x => x.PositionX == bullet.PositionX && x.PositionY == bullet.PositionY);
                if (killedEnemy != null)
                {
                    Enemies.Remove(killedEnemy);

                    mutex.WaitOne();
                    Console.SetCursorPosition(killedEnemy.PositionX, 31);
                    Console.Write('_');
                    mutex.ReleaseMutex();

                    GameScore++;
                    return true;
                }
                return false;
            }
            return false;
        }

        static void UpdateEnemyLocation()
        {
            var activeEnemies = new List<Entity>();

            if (Enemies != null)
            {
                foreach (var Enemy in Enemies)
                {
                    if (Enemy.PositionY != 30)
                    {
                        Enemy.PositionY += 1;
                        activeEnemies.Add(Enemy);
                    }
                    else if (Enemy.PositionY == 30)
                    {
                        EscapedEnemyCount++;
                        mutex.WaitOne();                        
                        Console.SetCursorPosition(Enemy.PositionX, 31);
                        Console.Write('_');
                        mutex.ReleaseMutex();
                    }
                }
            }
            Enemies = activeEnemies;
        }

        static void RenderHero(Entity hero)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (hero.PositionX < 0)
            {
                hero.PositionX = 0;
            }
            else if (hero.PositionX > 80)
            {
                hero.PositionX = 80;
            }
            Console.SetCursorPosition(hero.PositionX, hero.PositionY);
            Console.Write(hero.Symbol);
            Console.ResetColor();
        }

        static void RenderBullet(Entity bullet)
        {
            if (bullet.PositionY == 1)
            {
                RemoveOutOfBoundEntity(bullet);
                Thread.CurrentThread.Interrupt();
            }
            else
            {
                Console.SetCursorPosition(bullet.PositionX, bullet.PositionY);
                Console.Write(bullet.Symbol);
            }
        }

        static void RenderEnemies(List<Entity> Enemies)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (Enemies != null)
                {
                    foreach (var Enemy in Enemies)
                    {
                        if (Enemy.PositionY == 30)
                        {
                            RemoveOutOfBoundEntity(Enemy);
                        }
                        else
                        {
                            Console.SetCursorPosition(Enemy.PositionX, Enemy.PositionY);
                            Console.Write(Enemy.Symbol);
                            Console.SetCursorPosition(Enemy.PositionX, 31);
                            Console.Write('_');
                        }
                    }
                }
                Console.ResetColor();
            }
            catch { }
        }

        static void RemoveOutOfBoundEntity(Entity ent)
        {
            if (ent != null)
            {
                Console.SetCursorPosition(ent.PositionX, ent.PositionY);
                Console.Write(' ');
            }
        }

        static void UnRenderEntity(Entity entity)
        {
            Console.SetCursorPosition(entity.PositionX, entity.PositionY);
            Console.Write(' ');
        }

        static void UnRenderEnemies(List<Entity> Enemies)
        {
            try {
                if (Enemies != null)
                {
                    foreach (var Enemy in Enemies)
                    {

                        Console.SetCursorPosition(Enemy.PositionX, Enemy.PositionY);
                        Console.Write(' ');


                    }
                }
            }
            catch { }
        }
    }
}