using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1_SpaceInvaders.Models
{
    public class Entity
    {
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public char Symbol { get; }

        public Entity(int x, int y, char symbol)
        {
            PositionX = x;
            PositionY = y;
            Symbol = symbol;
        }
    }
}
