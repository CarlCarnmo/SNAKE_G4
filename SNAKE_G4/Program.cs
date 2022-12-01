using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

public static class Program
{
    public static bool hasWalls = false;
    public static async Task Main(string[] args)
    {
        // Calls the Menu Method.
        // L.84.
        Runmenu();
        var snakeGame = new SnakeGame();

        using (var cts = new CancellationTokenSource())
        {
            async Task MonitorKeyPresses()
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;
                        snakeGame.OnKeyPress(key);
                    }
                    await Task.Delay(1);
                }
            }
            var monitorKeyPresses = MonitorKeyPresses();
            do
            {
                // Hide cursor.
                Console.CursorVisible = false;
                // Calls this Methos each 'tick'.
                // L.308.
                snakeGame.OnGameTick();
                // Method to Render the Snake and playingfield.
                // L.327.
                snakeGame.Render();
                await Task.Delay(snakeGame._tickrate);
            } while (!snakeGame.GameOver);

            // Allow time for user to weep before application exits.
            for (var i = 0; i < 3; i++)
            {
                Console.Clear();
                await Task.Delay(500);
                snakeGame.Render();
                await Task.Delay(500);
            }
            cts.Cancel();
            await monitorKeyPresses;
        }
    }
    // Set playingfield size.
    private static void RunWalls()
    {
        Console.WriteLine("Vill du köra med eller utan väggar? y/n");
        string walls = Console.ReadLine();
        if (walls == "y")
            hasWalls = true;
        Console.WriteLine("Vilken storlek vill du ha på spelet?\n(s)mall\n(m)edium\n(l)arge");
        Console.Write(">");
        string command = Console.ReadLine().ToLower();
        if (command == "s")
        {
            Console.SetWindowSize(50, 10);
            if (hasWalls)
                Console.SetBufferSize(50, 10);
        }
        else if (command == "m")
        {
            Console.SetWindowSize(100, 20);
            if (hasWalls)
                Console.SetBufferSize(100, 20);
        }
        else if (command == "l")
        {
            Console.SetWindowSize(150, 30);
            if (hasWalls)
                Console.SetBufferSize(150, 30);
        }
    }

    public static void Runmenu()
    {
        string number;
        do
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║  Menyn:                                           ║");
            Console.WriteLine("║  Välja siffra nedan för att komma vidare i spelet ║");
            Console.WriteLine("║  1. Starta spelet                                 ║");
            Console.WriteLine("║  2. Avsluta Snake                                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝");
            Console.Write("> "); 
            number = Console.ReadLine();
            if (number == "1")
            {
                Console.Clear();
                Console.WriteLine("För att avsluta pågående spel, tryck Escape");
                RunWalls();
                return;
            }
            else if (number == "2")
            {
                Console.WriteLine("Välkommen åter");
                Environment.Exit(0);
            }
        }
        while (number != "2");
    }
}
enum Direction
{
    Up,
    Down,
    Left,
    Right
}
interface IRenderable
{
    void Render();
}
readonly struct Position
{
    public Position(int top, int left)
    {
        Top = top;
        Left = left;
    }
    public int Top { get; }
    public int Left { get; }
    public Position RightBy(int n) => new Position(Top, Left + n); //X
    public Position DownBy(int n) => new Position(Top + n, Left); //Y
}
class Apple : IRenderable
{
    public Apple(Position position)
    {
        Position = position;
    }
    public Position Position { get; }
    public void Render()
    {
        Console.SetCursorPosition(Position.Left, Position.Top);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.BackgroundColor= ConsoleColor.Green;
        Console.Write("A");
        Console.BackgroundColor= ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
    }
}
class Snake : IRenderable
{
    public static List<Position> _body;
    private int _growthSpurtsRemaining;

    //Spawn condition for _Snake.
    public Snake(Position spawnLocation, int initialSize = 1)
    {
        _body = new List<Position> { spawnLocation };
        _growthSpurtsRemaining = Math.Max(0, initialSize - 1);
        Dead = false;
    }
    public bool Dead { get; private set; }
    public Position Head => _body.First();
    private IEnumerable<Position> Body => _body.Skip(1);

    // Movement Method for _Snake.
    public void Move(Direction direction)
    {
        if (Dead) throw new InvalidOperationException();
        Position newHead;
        switch (direction)
        {
            case Direction.Up:
                newHead = Head.DownBy(-1);
                break;
            case Direction.Left:
                newHead = Head.RightBy(-1);
                break;
            case Direction.Down:
                newHead = Head.DownBy(1);
                break;
            case Direction.Right:
                newHead = Head.RightBy(1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        if (Program.hasWalls == true)
        {
            if (_body.Contains(newHead) || !PositionIsValid(newHead))
            {
                Dead = true;
                return;
            }
        }
        else if (Program.hasWalls == false)
        {
            if (_body.Contains(newHead) || !PositionIsValid2(newHead))
            {
                Dead = true;
                return;
            }
        }
        _body.Insert(0, newHead);
        if (_growthSpurtsRemaining > 0)
        {
            _growthSpurtsRemaining--;
        }
        else
        {
            _body.RemoveAt(_body.Count - 1);
        }
    }
    public void Grow()
    {
        if (Dead) throw new InvalidOperationException();

        _growthSpurtsRemaining++;
    }
    // Method to render the snake.
    public void Render() 
    {
        Console.SetCursorPosition(Head.Left, Head.Top);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("@"); //Head
        Console.ForegroundColor = ConsoleColor.White;


        foreach (var position in Body)
        {
            Console.SetCursorPosition(position.Left, position.Top);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.Write("W");//Body
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    private static bool PositionIsValid(Position position) => 
        position.Top >= 1 && position.Top < Console.WindowHeight - 1 && position.Left >= 1 && position.Left < Console.WindowWidth - 1;
    private static bool PositionIsValid2(Position position) =>
      position.Top >= 0 && position.Top < Console.WindowHeight && position.Left >= 0 && position.Left < Console.WindowWidth;
}
class SnakeGame : IRenderable
{
    // Start POS.
    private static readonly Position Origin = new Position(Console.WindowHeight/2, Console.WindowWidth/2);
    private Direction _currentDirection;
    private Direction _nextDirection;
    private Snake _snake;
    private Apple _apple;
    public TimeSpan _tickrate;
    public SnakeGame()
    {
        // Start conditions.
        _snake = new Snake(Origin, initialSize: 5);
        _apple = CreateApple();
        _currentDirection = Direction.Right;
        _nextDirection = Direction.Right;
        _tickrate = TimeSpan.FromMilliseconds(150);
    }
    // Murderes the snake IF gameover.
    public bool GameOver => _snake.Dead;
    public void OnKeyPress(ConsoleKey key)
    {
        Direction newDirection;

        switch (key)
        {
            case ConsoleKey.UpArrow:
                newDirection = Direction.Up;
                break;

            case ConsoleKey.LeftArrow:
                newDirection = Direction.Left;
                break;

            case ConsoleKey.DownArrow:
                newDirection = Direction.Down;
                break;

            case ConsoleKey.RightArrow:
                newDirection = Direction.Right;
                break;

            case ConsoleKey.Escape:
                newDirection = Direction.Right;
                Console.Clear();
                Environment.Exit(0);
                break;

            default:
                return;
        }
        // Snake cannot turn 180 degrees.
        if (newDirection == OppositeDirectionTo(_currentDirection))
        {
            return;
        }
        _nextDirection = newDirection;
    }
    // TickTackToe.
    public void OnGameTick()
    {
        if (GameOver) throw new InvalidOperationException();

        _currentDirection = _nextDirection;
        _snake.Move(_currentDirection);

        // If the snake's head moves to the same position as an apple, the snake
        // eats it.
        if (_snake.Head.Equals(_apple.Position))
        {
            _snake.Grow();
            _apple = CreateApple();
            if (_tickrate.TotalMilliseconds > 25) { _tickrate = TimeSpan.FromMilliseconds(_tickrate.TotalMilliseconds - 5); }
            if (_tickrate.TotalMilliseconds < 100) { _tickrate = TimeSpan.FromMilliseconds(_tickrate.TotalMilliseconds - 2.5); }
        }
    }
    public void Render()
    {
        Console.Clear();
        // Walls for playingfield.
        if (Program.hasWalls == true)
        {
            string lines = "";
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write("═");
            }
            for (int i = 2; i < Console.WindowHeight; i++)
            {
                Console.WriteLine($"║{lines.PadRight(Console.WindowWidth - 2, ' ')}║");
            }
            for (int i = Console.WindowHeight; i < Console.WindowWidth + Console.WindowHeight; i++)
            {
                Console.Write("═");
            }
        }
        _snake.Render();
        _apple.Render();
        Console.SetCursorPosition(0, 0);
    }
    private static Direction OppositeDirectionTo(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
            case Direction.Down: return Direction.Up;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    // Method to spawn the apple on playingfield.
    private static Apple CreateApple()
    {
        // Can be factored elsewhere.
        int numberOfRows = Console.WindowHeight;
        int numberOfColumns = Console.WindowWidth;
        var random = new Random();
        var top = random.Next(1, numberOfRows - 1);
        var left = random.Next(1, numberOfColumns - 1);
        var position = new Position(top, left);
        var apple = new Apple(position);
        return apple;
    }
}