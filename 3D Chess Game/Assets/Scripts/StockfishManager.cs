using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
}
