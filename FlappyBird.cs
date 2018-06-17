using System;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperNEAT
{
    class FlappyBird
    {
        public double VerticalInitialPosition = 50;
        public double VerticalVelocity = -10;
        public double VerticalTime = 0;
        public double Gravity = 4.9;

        public double HorizVelocity = 20;
        public double HorizTime = 0;

        public double VerticalPosition;
        public double HorizPosition;
        public void Jump()
        {
            VerticalTime = 0;
            VerticalInitialPosition = VerticalPosition;
        }
        public void DisplayFrame(Graphics g, double playerX, double playerY)
        {
            System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green);
            g.FillRectangle(myBrush, (float)playerX, (float)playerY, 5, 5);
        }     
        public double GetY()
        {
            return Gravity * Math.Pow(VerticalTime, 2) + VerticalVelocity * VerticalTime + VerticalInitialPosition;
        }
        public double GetX()
        {
            return HorizVelocity * HorizTime;
        }
        public void RunFrame(Graphics g)
        {
            double y = GetY();
            double x = GetX();
            VerticalPosition = y;
            HorizPosition = x;
            DisplayFrame(g, x, y);
            VerticalTime += .1;
            HorizTime += .1;
        }
    }
}
