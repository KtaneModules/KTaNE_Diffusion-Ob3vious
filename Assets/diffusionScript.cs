using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class diffusionScript : MonoBehaviour {

    //public stuff
    public KMAudio Audio;
    public List<KMSelectable> Buttons;
    public TextMesh Text;
    public KMBombModule Module;

    //private stuff
    private List<int> states = new List<int> { };
    private List<int> diffused;
    private List<int> autosolve;
    private bool solved;

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < 12; i++)
        {
            states.Add(0);
            int x = i;
            Buttons[i].OnHighlight += delegate { if (!solved) { Buttons[x].GetComponent<MeshRenderer>().material.color = new Color((states[x] == 1 ? 1 : 0), 0, (states[x] == 2 ? 1 : 0)); Text.text = (diffused[x] % 5).ToString() + (diffused[x] / 5).ToString() + "\n" + "0AB"[states[x]]; } };
            Buttons[i].OnHighlightEnded += delegate { if (!solved) { Buttons[x].GetComponent<MeshRenderer>().material.color = new Color((diffused[x] % 5) / 4f, 0, (diffused[x] / 5) / 4f); Text.text = ""; } };
            Buttons[i].OnInteract += delegate
            {
                if (!solved)
                {
                    Buttons[x].AddInteractionPunch(1f);
                    states[x] = (states[x] + 1) % 3;
                    Buttons[x].OnHighlight();
                    CheckSolve();
                }
                return false;
            };
        }
    }

    void Start()
    {
        GenerateSolution();
        for (int i = 0; i < 12; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color((diffused[i] % 5) / 4f, 0, (diffused[i] / 5) / 4f);
    }
    
    private void GenerateSolution()
    {
        panic:
        List<int> prediff = new List<int> { };
        for (int i = 0; i < 12; i++)
            prediff.Add(Rnd.Range(0, 3));
        if (prediff.Sum() == 0)
            goto panic;
        List<int> postdiff = new List<int> { };
        for (int i = 0; i < 12; i++)
            postdiff.Add((prediff[i] == 2 ? 5 : prediff[i]) * 2 + (prediff[(i + 1) % 12] == 2 ? 5 : prediff[(i + 1) % 12]) + (prediff[(i + 11) % 12] == 2 ? 5 : prediff[(i + 11) % 12]));
        diffused = postdiff;
        Debug.LogFormat("[Diffusion #{0}] The result of diffusion (from tl cw) is {1}.", _moduleID, diffused.Select(x => new string[] { "A0B0", "A1B0", "A2B0", "A3B0", "A4B0", "A0B1", "A1B1", "A2B1", "A3B1", "A4B1", "A0B2", "A1B2", "A2B2", "A3B2", "A4B2", "A0B3", "A1B3", "A2B3", "A3B3", "A4B3", "A0B4", "A1B4", "A2B4", "A3B4", "A4B4" }[x]).Join(", "));
        Debug.LogFormat("[Diffusion #{0}] A possible solution is {1}.", _moduleID, prediff.Select(x => new string[] { "0", "A", "B" }[x]).Join(", "));
        autosolve = prediff;
    }

    private void CheckSolve()
    {
        List<int> prediff = states;
        List<int> postdiff = new List<int> { };
        for (int i = 0; i < 12; i++)
            postdiff.Add((prediff[i] == 2 ? 5 : prediff[i]) * 2 + (prediff[(i + 1) % 12] == 2 ? 5 : prediff[(i + 1) % 12]) + (prediff[(i + 11) % 12] == 2 ? 5 : prediff[(i + 11) % 12]));
        bool good = true;
        for (int i = 0; i < 12; i++)
            if (postdiff[i] != diffused[i])
                good = false;
        if (good)
        {
            for (int i = 0; i < 12; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color((states[i] == 1 ? 1 : 0), 0, (states[i] == 2 ? 1 : 0));
            Text.text = "";
            Debug.LogFormat("[Diffusion #{0}] Module solved!", _moduleID);
            Module.HandlePass();
            solved = true;
            StartCoroutine(Solve());
            Audio.PlaySoundAtTransform("Solve", Module.transform);
        }
        else
        {
            Audio.PlaySoundAtTransform("Click", Module.transform);
        }
    }

    private IEnumerator Solve()
    {
        List<Color> colours = new List<Color> { };
        for (int i = 0; i < 12; i++)
            colours.Add(Buttons[i].GetComponent<MeshRenderer>().material.color);
        while (true)
        {
            yield return new WaitForSeconds(0.25f);
            List<Color> colours2 = new List<Color> { };
            for (int i = 0; i < 12; i++)
            {
                colours2.Add(Color.Lerp(colours[i], Color.Lerp(colours[(i + 1) % 12], colours[(i + 11) % 12], 0.5f), 0.5f));
                Buttons[i].GetComponent<MeshRenderer>().material.color = colours2.Last();
            }
            colours = colours2;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} set 1 A 12 0' to set those positions from top left clockwise. '!{0} inspect 1 2' to highlight them and inspect their values.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (Regex.IsMatch(command, @"^set(\s(1|2|3|4|5|6|7|8|9|10|11|12)\s(0|a|b))+$"))
        {
            MatchCollection matches = Regex.Matches(command.Replace("set", ""), @"(1|2|3|4|5|6|7|8|9|10|11|12)\s(0|a|b)");
            foreach (Match match in matches)
            {
                Debug.Log(match.ToString());
                string subcmd = match.ToString();
                while ("0ab"[states[int.Parse(subcmd.Split(' ')[0]) - 1]] != subcmd.Split(' ')[1][0])
                {
                    Buttons[int.Parse(subcmd.Split(' ')[0]) - 1].OnInteract();
                    Buttons[int.Parse(subcmd.Split(' ')[0]) - 1].OnHighlightEnded();
                    yield return null;
                }
            }
            yield return "solve";
        }
        else if(Regex.IsMatch(command, @"^inspect(\s(1|2|3|4|5|6|7|8|9|10|11|12))+$"))
        {
            MatchCollection matches = Regex.Matches(command.Replace("inspect", ""), @"(1|2|3|4|5|6|7|8|9|10|11|12)");
            foreach (Match match in matches)
            {
                Debug.Log(match.ToString());
                Buttons[int.Parse(match.ToString()) - 1].OnHighlight();
                yield return new WaitForSeconds(0.75f);
                Buttons[int.Parse(match.ToString()) - 1].OnHighlightEnded();
            }
            yield return "solve";
        }
        else
            yield return "sendtochaterror Invalid command.";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        for (int i = 0; i < 12; i++)
            while (states[i] != autosolve[i])
            {
                Buttons[i].OnInteract();
                Buttons[i].OnHighlightEnded();
                yield return true;
            }
    }
}
