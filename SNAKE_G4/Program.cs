using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class Program
{
    public static bool hasWalls = false;
    public static async Task Main(string[] args)
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
        var tickRate = TimeSpan.FromMilliseconds(75); //Hur ofta programmet uppdateras. Denna kan ändras så det ser ut som att ormen
                                                      //rör sig snabbare/långsammare
        var snakeGame = new SnakeGame();

        using (var cts = new CancellationTokenSource()) //metod för keypress.
        {
            async Task MonitorKeyPresses()
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true).Key;
                        snakeGame.OnKeyPress(key); //läser av vilken knapp som tryckts
                    }
                    await Task.Delay(1); //Gör tasken efter X millisekunder
                }
            }
            var monitorKeyPresses = MonitorKeyPresses(); //om en knapp trycks
            do
            {
                snakeGame.OnGameTick(); //anropar metod som körs varje gång spelet uppdateras (görs 
                snakeGame.Render(); //skapar storleken på ormen ovh äpplet
                await Task.Delay(tickRate); //denna do-loop körs efter 100ms
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
}
enum Direction //enum directions
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
readonly struct Position //poistion
{
    public Position(int top, int left)
    {
        Top = top;
        Left = left;
    }
    public int Top { get; }
    public int Left { get; }
    public Position RightBy(int n) => new Position(Top, Left + n); //körs sidleds
    public Position DownBy(int n) => new Position(Top + n, Left); //körs vertikalt
}
class Apple : IRenderable
{
    public Apple(Position position)
    {
        Position = position;
    }
    public Position Position { get; }
    public void Render() //renderar äpplet
    {
        Console.SetCursorPosition(Position.Left, Position.Top);
        Console.Write("A"); //äpplet ser ut som ett A
    }
}
class Snake : IRenderable
{
    public static List<Position> _body; //
    private int _growthSpurtsRemaining;
    public Snake(Position spawnLocation, int initialSize = 1) //spawnar ormen på plats spawnLocation med storlek 1
    {
        _body = new List<Position> { spawnLocation };
        _growthSpurtsRemaining = Math.Max(0, initialSize - 1); //hur stor ormen är
        Dead = false; //ormen är inte död
    }
    public bool Dead { get; private set; }
    public Position Head => _body.First(); //sätter huvudet först
    private IEnumerable<Position> Body => _body.Skip(1);
    public void Move(Direction direction) //metod för att röra sig
    {
        if (Dead) throw new InvalidOperationException();
        Position newHead;
        switch (direction)
        {
            case Direction.Up:
                newHead = Head.DownBy(-1); //om upp så rör sig den downby-1 dvs upp
                break;

            case Direction.Left://om left så rör den sig rightby -1 dvs left
                newHead = Head.RightBy(-1);
                break;

            case Direction.Down: //om down så rör den sig downby 1 dvs down
                newHead = Head.DownBy(1);
                break;

            case Direction.Right: //om right så rör den sig rightby1 dvs right
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
    public void Grow() //metod så ormen växer
    {
        if (Dead) throw new InvalidOperationException();

        _growthSpurtsRemaining++;
    }
    public void Render() //metod för att visa ormen
    {
        Console.SetCursorPosition(Head.Left, Head.Top);
        Console.Write("@");//huvudet


        foreach (var position in Body) //kroppen
        {
            Console.SetCursorPosition(position.Left, position.Top);
            Console.Write("#");
        }
    }
    private static bool PositionIsValid(Position position) => 
        position.Top >= 1 && position.Top < Console.WindowHeight - 1 && position.Left >= 1 && position.Left < Console.WindowWidth - 1; //Ändrat så ormen dör när den åker in en vägg
    private static bool PositionIsValid2(Position position) =>
      position.Top >= 0 && position.Top < Console.WindowHeight && position.Left >= 0 && position.Left < Console.WindowWidth; //Ändrat så ormen dör när den åker in en vägg
}
class SnakeGame : IRenderable
{
    private static readonly Position Origin = new Position(Console.WindowHeight / 2, Console.WindowWidth / 2); //plats där ormen startar
    private Direction _currentDirection; //rör sig åt ett hål initiexlt
    private Direction _nextDirection; //
    private Snake _snake; //skapar ormen
    private Apple _apple; //skapar ett äpple
    public SnakeGame()
    {
        _snake = new Snake(Origin, initialSize: 5); //startplats och storlek
        _apple = CreateApple(); //skapar ett äpple
        _currentDirection = Direction.Right; //fyller ingen funktion när spelet startar
        _nextDirection = Direction.Right; //startriktning
    }
    public bool GameOver => _snake.Dead; //om game over så snake död 
    public void OnKeyPress(ConsoleKey key) //metod som registrerar och sätter direktions
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
    public void OnGameTick() //Detta händer varje tick
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
        }
    }
    public void Render() //detta renderas
    {
        Console.Clear();
        // Lagt till väggar
        if (Program.hasWalls == true)
        {
            string lines = "";
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write("-");
            }
            for (int i = 2; i < Console.WindowHeight; i++)
            {
                Console.WriteLine($"|{lines.PadRight(Console.WindowWidth - 2, ' ')}|");
            }
            for (int i = Console.WindowHeight; i < Console.WindowWidth + Console.WindowHeight; i++)
            {
                Console.Write("-");
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
    private static Apple CreateApple() //skapar ett äpple
    {
        // Can be factored elsewhere.
        int nrRow = Console.WindowHeight;
        int numberOfRows = Console.WindowHeight;
        int numberOfColumns = Console.WindowWidth;
        var random = new Random();
        var top = random.Next(1, numberOfRows - 1); //Tagit bort +1 då äpplet kunde spawna ett steg utanför spelfältet
        var left = random.Next(1, numberOfColumns - 1); //Tagit bort +1 då äpplet kunde spawna ett steg utanför spelfältet
        var position = new Position(top, left);
        var apple = new Apple(position);
        return apple;


    }
}