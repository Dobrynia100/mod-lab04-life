using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using cli_life;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Threading;
using System.IO;
using System.Text.Json;
namespace NET
{
    [TestClass]
    public class UnitTest1
    {
        private Board board;
        [TestMethod]
        public void TestDead()
        {
            Board board= new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                false,
                liveDensity: 0.5);
            board.dead();
            bool check=true;
            foreach (var cell in board.Cells)
               if(cell.IsAlive)
                   check = false;
                    
            Assert.IsTrue(check);
        }
        [TestMethod]
        public void TestMethod2()
        {
            Reset();
            Assert.AreEqual(board.Width, 50);
            Assert.AreEqual(board.Height, 20);
            Assert.AreEqual(board.CellSize, 1);
        }
        
        [TestMethod]
        public void TestLoad()
        {
            Reset();
            Load(ref board, "3.dat");
            var cells = board.Cells;            
            Assert.IsTrue(cells[0, 3].IsAlive);
            Assert.IsTrue(cells[1, 1].IsAlive);
            Assert.IsTrue(cells[1, 2].IsAlive);
            Assert.IsTrue(cells[1, 11].IsAlive);
            Assert.IsTrue(cells[49, 4].IsAlive);
        }
        [TestMethod]
        public void TestAdvance()
        {
            Reset();
            Load(ref board, "3.dat");            
            var cells = board.Cells;
            board.Advance();
            Assert.IsTrue(cells[1, 12].IsAlive);
            Assert.IsTrue(cells[40, 1].IsAlive);
            Assert.IsTrue(cells[40, 19].IsAlive);
            Assert.IsTrue(cells[49, 4].IsAlive);
        }
        [TestMethod]
        public void TestSettings()
        {
            Reset();
            LoadSettings("board_settings.json");
           
            Assert.AreEqual(board.Width, 50);
            Assert.AreEqual(board.Height, 20);
            Assert.AreEqual(board.CellSize, 1);
        }
        public void Reset()
        {
            board = new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                false,
                liveDensity: 0.5);
        }
        public static void Load(ref Board board, string filename) {
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
        }
        public static void Save(Board board, string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int x = 0; x < board.Columns; x++)
                {
                    for (int y = 0; y < board.Rows; y++)
                    {
                        if (board.Cells[x, y].IsAlive)
                        {
                            writer.Write("{0},{1},", x, y);
                        }
                    }
                }
            }
        }
        public static Board LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл настроек не найден", filePath);

            string jsonString = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<BoardSettings>(jsonString);

            var board = new Board(settings.Width, settings.Height, settings.CellSize, false, settings.LiveDensity);
            return board;
        }
    }
}
