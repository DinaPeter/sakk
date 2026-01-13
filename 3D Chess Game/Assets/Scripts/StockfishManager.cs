using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

[Serializable]
public class StockfishMove
{
    public string move;
    public int eval;
}

public class StockfishManager : MonoBehaviour
{
    private Process stockfish;
    private StreamWriter input;
    private StreamReader output;

    void Start()
    {
        StartStockfish();
    }

    void OnDestroy()
    {
        StopStockfish();
    }

    void StartStockfish()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "stockfish.exe");

        stockfish = new Process();
        stockfish.StartInfo.FileName = path;
        stockfish.StartInfo.UseShellExecute = false;
        stockfish.StartInfo.RedirectStandardInput = true;
        stockfish.StartInfo.RedirectStandardOutput = true;
        stockfish.StartInfo.CreateNoWindow = true;

        stockfish.Start();

        input = stockfish.StandardInput;
        output = stockfish.StandardOutput;

        input.WriteLine("uci");
        input.Flush();

        // VÁRUNK uciok-ig
        while (true)
        {
            string line = output.ReadLine();
            if (line == "uciok")
                break;
        }
    }

    public async Task<string> GetBestMove(string fen, int thinkTimeMs = 1000)
    {
        input.WriteLine($"position fen {fen}");
        input.WriteLine($"go movetime {thinkTimeMs}");
        input.Flush();

        while (true)
        {
            string line = await output.ReadLineAsync();
            if (line.StartsWith("bestmove"))
            {
                return line.Split(' ')[1]; // pl. e2e4
            }
        }
    }

    void SendCommand(string command)
    {
        if (input == null) return;

        input.WriteLine(command);
        input.Flush();
    }

    public void SetSkillLevel(int level)
    {
        SendCommand($"setoption name Skill Level value {level}");
    }

    void StopStockfish()
    {
        if (stockfish != null && !stockfish.HasExited)
        {
            input.WriteLine("quit");
            stockfish.Kill();
        }
    }

    public async Task<List<StockfishMove>> GetTopMoves(string fen, int count)
    {
        List<StockfishMove> moves = new List<StockfishMove>();

        SendCommand($"setoption name MultiPV value {count}");
        SendCommand("ucinewgame");
        SendCommand($"position fen {fen}");
        SendCommand("go depth 12");

        while (true)
        {
            string line = await output.ReadLineAsync();
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.Contains(" multipv ") && line.Contains(" pv "))
            {
                StockfishMove move = ParseInfoLine(line);

                if (move != null && !moves.Exists(m => m.move == move.move))
                {
                    moves.Add(move);
                }
            }

            if (line.StartsWith("bestmove"))
                break;
        }

        moves.Sort((a, b) => b.eval.CompareTo(a.eval));

        return moves;
    }

    private StockfishMove ParseInfoLine(string line)
    {
        try
        {
            string[] parts = line.Split(' ');

            int pvIndex = Array.IndexOf(parts, "pv");
            int scoreIndex = Array.IndexOf(parts, "score");

            if (pvIndex == -1 || scoreIndex == -1 || pvIndex + 1 >= parts.Length)
                return null;

            string move = parts[pvIndex + 1];
            int eval = 0;

            // score cp X  OR score mate X
            if (parts[scoreIndex + 1] == "cp")
            {
                int.TryParse(parts[scoreIndex + 2], out eval);
            }
            else if (parts[scoreIndex + 1] == "mate")
            {
                int mate = int.Parse(parts[scoreIndex + 2]);
                eval = mate > 0 ? 100000 : -100000;
            }

            return new StockfishMove
            {
                move = move,
                eval = eval
            };
        }
        catch
        {
            return null;
        }
    }
}
