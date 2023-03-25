using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using cli_life;
namespace NET
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
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
        
    }
}
