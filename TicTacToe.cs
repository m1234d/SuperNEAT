using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperNEAT
{
    class TicTacToe
    {
        public bool IsRunning = true;
        public string Turn = "X";
        public bool isTie = false;
        public double[][] board = new double[3][];
        public static bool training = true;
        public void CreateGame()
        {
            board = new double[3][];
            for(int i = 0; i < board.Length; i++)
            {
                board[i] = new double[] { 0, 0, 0 };
            }
            Turn = "X";
            isTie = false;
            IsRunning = true;
        }
        public List<double> GetBoard()
        {
            List<double> boardList = new List<double>();
            for(int i = 0; i < board.Length; i++)
            {
                for(int p = 0; p < board[i].Length; p++)
                {
                    boardList.Add(board[i][p]);
                }
            }
            return boardList;
        }
        public void SetBoard(double move, double value)
        {
            board[(int)(move / 3)][(int)(move % 3)] = value;
        }
        public bool IsValidMove(double move)
        {
            List<double> boardList = GetBoard();

            if(boardList[(int)move] == 0)
            {
                return true;
            }
            return false;
        }
        public void PlayMove(double move)
        {
            if(Turn == "X")
            {
                SetBoard(move, 1);
                if(GameOver())
                {
                    IsRunning = false;
                    Turn = "X";
                }
                else
                {
                    Turn = "O";
                }

            }
            else
            {
                SetBoard(move, -1);
                if(GameOver())
                {
                    IsRunning = false;
                    Turn = "X";
                }
                else
                {
                    Turn = "X";
                }
            }

        }
        public bool GameOver()
        {
            for(int i = 0; i < board.Length; i++)
            {
                if(board[i][0] == board[i][1] && board[i][0] == board[i][2] && board[i][0] != 0)
                {
                    return true;
                }
            }
            for(int i = 0; i < board[0].Length; i++)
            {
                if(board[0][i] == board[1][i] && board[0][i] == board[2][i] && board[0][i] != 0)
                {
                    return true;
                }
            }
            if(board[0][0] == board[1][1] && board[0][0] == board[2][2] && board[0][0] != 0)
            {
                return true;
            }
            if(board[0][2] == board[1][1] && board[0][2] == board[2][0] && board[0][2] != 0)
            {
                return true;
            }
            foreach(double d in GetBoard())
            {
                if(d == 0)
                {
                    return false;
                }
            }
            isTie = true;
            return true;
        }
        public void PlayRandom(Random r)
        {
            bool playing = true;
            while (playing)
            {
                int move = r.Next(0, 9);
                if(IsValidMove(move))
                {
                    playing = false;
                    SetBoard(move, 1);
                    if (GameOver())
                    {
                        IsRunning = false;
                    }
                    else
                    {
                        Turn = "O";
                    }
                }
            }
        }
    }
}
