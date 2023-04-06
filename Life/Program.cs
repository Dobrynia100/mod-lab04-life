using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cli_life
{
    public class Cell
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class BoardSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, bool downl, double liveDensity = .1)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();

            if(!downl)Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        public void dead()
        {
            foreach (var cell in Cells)
                cell.IsAlive = false;
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++) {
                for (int y = 0; y < Rows; y++) {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;
                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public void SaveSettings(string filePath) {
            var settings = new BoardSettings
            {
                Width = Columns,
                Height = Rows,
                CellSize = CellSize,
                LiveDensity = 0.5
            };

            string jsonString = JsonSerializer.Serialize(settings);
            File.WriteAllText(filePath, jsonString);
        }

        public static Board LoadSettings(string filePath) {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл настроек не найден", filePath);

            string jsonString = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<BoardSettings>(jsonString);

            var board = new Board(settings.Width, settings.Height, settings.CellSize,false, settings.LiveDensity);
            return board;
        }
        public void Classify()
        {
            int rows = Rows;
            int cols = Columns;
            bool[,] classification = new bool[rows, cols];

            bool[,] block = new bool[2, 2] { { true, true }, { true, true } };

            for (int r = 0; r < rows - block.GetLength(0); r++)
            {
                for (int c = 0; c < cols - block.GetLength(1); c++)
                {
                    bool match = true;
                    for (int br = 0; br < block.GetLength(0); br++)
                    {
                        for (int bc = 0; bc < block.GetLength(1); bc++)
                        {
                            if (Cells[r + br, c + bc].IsAlive != block[br, bc])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (!match)
                            break;
                    }
                    if (match)
                    {
                        for (int br = 0; br < block.GetLength(0); br++)
                        {
                            for (int bc = 0; bc < block.GetLength(1); bc++)
                            {
                                classification[r + br, c + bc] = true;
                            }
                        }
                    }
                }
            }

            for (int r = 0; r < rows; r++)
            {
                StringBuilder sb = new StringBuilder();
                for (int c = 0; c < cols; c++)
                {
                    if (classification[r, c])
                    {
                        sb.Append("*");
                    }
                    else
                    {
                        sb.Append(Cells[r, c].IsAlive ? "O" : ".");
                    }
                }
                Console.WriteLine(sb.ToString());
            }
        }
        public int GetStablePhaseTime(int maxIterations)
        {
            int sumI = 0;
            int Iter= 0;

            while (Iter < maxIterations)
            {
                Advance();
                int stablePhaseTime = StablePhaseTime();

                if (stablePhaseTime > 0)
                {
                    sumI += stablePhaseTime;
                    Iter++;
                }
            }

            return Iter > 0 ? sumI / Iter : 0;
        }

      

        private int StablePhaseTime()
        {
            var boardState = JsonSerializer.Serialize(Cells);
            int iterations = 0;

            while (true)
            {
                Advance();
                string newBoardState = JsonSerializer.Serialize(Cells);

                if (newBoardState == boardState)
                {
                    return iterations;
                }

                boardState = newBoardState;
                iterations++;
            }
        }
        public int SymCount()
        {
            int count = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (Cells[r, c].IsAlive && Cells[Rows - 1 - r, Columns - 1 - c].IsAlive)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public void Symmetry(int numgen)
        {
            var board1 = this.Clone();
            var board2 = this.Clone();
            for (int i = 0; i < numgen; i++)
            {
                board1.Advance();
                board2.Advance();
                board2.Mirror();
                if (board1.Equals(board2))
                {
                    Console.WriteLine($"Симметрия после {i + 1} итерраций.");
                    return;
                }
            }
            Console.WriteLine($"асимметрия после {numgen} итерраций.");
        }

        private void Mirror()
        {
            int rows = Rows;
            int Colums = Columns;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < Colums / 2; c++)
                {
                    bool temp = Cells[r, c].IsAlive;
                    Cells[r, c].IsAlive = Cells[r, Colums - 1 - c].IsAlive;
                    Cells[r, Colums - 1 - c].IsAlive = temp;
                }
            }
        }

        public Board Clone()
        {
            var clone = new Board(Width, Height, CellSize,false);
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    clone.Cells[x, y].IsAlive = Cells[x, y].IsAlive;
            return clone;
        }
    }
    class Program
    {
        static Board board;
        static private void Reset() {
            board = new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                false,
                liveDensity: 0.5);
        }
        static void Render() {
            for (int row = 0; row < board.Rows; row++) {
                for (int col = 0; col < board.Columns; col++) {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive) {
                        Console.Write('*');
                    }
                    else {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }

        public static void Save(Board board, string filename) {
            using (StreamWriter writer = new StreamWriter(filename)) {
                for (int x = 0; x < board.Columns; x++) {
                    for (int y = 0; y < board.Rows; y++) {
                        if (board.Cells[x, y].IsAlive) {
                            writer.Write("{0},{1},", x, y);
                        }
                    }
                }
            }
        }

        public static void Load(ref Board board, string filename) {
            try
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    board = new Board(board.Width, board.Height, board.CellSize, true);
                    board.dead();
                    int i = 1;
                    string[] coordinates = reader.ReadLine().Split(',');
                    while (i! <= coordinates.Length - 1)
                    {
                        int x = int.Parse(coordinates[i - 1]);
                        int y = int.Parse(coordinates[i]);
                        board.Cells[x, y].IsAlive = true;
                        i += 2;
                    }
                }
            }catch (Exception e) {
                Console.WriteLine("Файл не найден");
                    }
        }
        public static int countalive() {
            int count = 0;
            for (int x = 0; x < board.Columns; x++) {
                for (int y = 0; y < board.Rows; y++) {
                    if (board.Cells[x, y].IsAlive) {
                        count++;
                    }
                }
            }
            return count;
        }

        static void Main(string[] args)
        {
            Reset();
            bool work = true;
            while(true)
            {
                if (work)
                {
                    Console.Clear();
               
                    Render();
                    board.Advance();
                    Thread.Sleep(1000);
                }
               
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.KeyChar)
                    {

                        case 's':
                            Console.WriteLine("сохранение");
                            Save(board, "board.dat");
                            Console.WriteLine("Успешное сохранение в board.dat");
                            break;
                        case 'l':
                            Console.WriteLine("Загрузка");
                            Load(ref board, "board.dat");
                            Console.WriteLine("Загружено из board.dat");
                            break;
                        case 'p':
                            Console.WriteLine("Пауза");
                            work = false;
                            break;
                        case 'n':
                            Console.WriteLine("сохранение настроек");
                            board.SaveSettings("board_settings.json");
                            break;
                        case 'd':
                            board = Board.LoadSettings("board_settings.json");
                            Console.WriteLine("Загрузка настроек");
                            break;
                        case 'r':
                            Console.WriteLine("Возобновление");
                            work = true;
                            break;

                    }
                }
                

            }
        }
    }
}