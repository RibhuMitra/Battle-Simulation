using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text.RegularExpressions;

public class UnityLogForwarder : MonoBehaviour
{
    public string serverUrl = "http://localhost:5000/api/logs";

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Skip logs that aren't ours
        if (!logString.Contains("Pos:") && !logString.Contains("HP =") && !logString.Contains("Died"))
            return;

        StartCoroutine(SendLogCoroutine(logString));
    }

    private IEnumerator SendLogCoroutine(string logText)
    {
        string json = ParseLogToJson(logText);
        if (string.IsNullOrEmpty(json))
            yield break;

        var request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            // Do not debug log here to prevent an infinite loop of console logs!
        }
    }

    private string ParseLogToJson(string log)
    {
        // 1. Check for Death Log
        // Format: "RomanSoldier Died"
        if (log.EndsWith(" Died"))
        {
            string name = log.Substring(0, log.Length - 5);
            return $"{{\"name\":\"{name}\",\"isDead\":true}}";
        }

        // 2. Check for Health Log
        // Format: "RomanSoldier HP = 80"
        if (log.Contains(" HP = "))
        {
            var match = Regex.Match(log, @"^(?<name>[\w\s()]+?)\s+HP\s+=\s+(?<hp>-?\d+(\.\d+)?)");
            if (match.Success)
            {
                return $"{{\"name\":\"{match.Groups["name"].Value.Trim()}\",\"hp\":{match.Groups["hp"].Value}}}";
            }
        }

        // 3. Check for Brain state logs
        // Format: "RomanSoldier (Moving) | Current Pos: (0.00, 1.00, 0.00) | Target Pos: (5.00, 1.00, 5.00) | Distance: 7.07"
        var nameStateMatch = Regex.Match(log, @"^(?<name>[\w\s()]+?)\s+\((?<state>\w+)\)\s+\|\s+Current Pos:\s+\((?<cx>-?\d+(\.\d+)?),\s+(?<cy>-?\d+(\.\d+)?),\s+(?<cz>-?\d+(\.\d+)?)\)");
        if (nameStateMatch.Success)
        {
            string name = nameStateMatch.Groups["name"].Value.Trim();
            string state = nameStateMatch.Groups["state"].Value;
            string cx = nameStateMatch.Groups["cx"].Value;
            string cz = nameStateMatch.Groups["cz"].Value;

            string targetX = "null";
            string targetZ = "null";
            string distance = "null";

            // Extract target position if present
            var targetPosMatch = Regex.Match(log, @"Target Pos:\s+\((?<tx>-?\d+(\.\d+)?),\s+(?<ty>-?\d+(\.\d+)?),\s+(?<tz>-?\d+(\.\d+)?)\)");
            if (targetPosMatch.Success)
            {
                targetX = targetPosMatch.Groups["tx"].Value;
                targetZ = targetPosMatch.Groups["tz"].Value;
            }

            // Extract distance if present
            var distMatch = Regex.Match(log, @"Distance(?:\s+to\s+Enemy)?:\s+(?<dist>\d+(\.\d+)?)");
            if (distMatch.Success)
            {
                distance = distMatch.Groups["dist"].Value;
            }

            // Faction resolution
            string faction = "Neutral";
            if (name.ToLower().Contains("roman")) faction = "Roman";
            else if (name.ToLower().Contains("carthage")) faction = "Carthage";

            return $"{{\"name\":\"{name}\",\"faction\":\"{faction}\",\"state\":\"{state}\",\"x\":{cx},\"z\":{cz},\"tx\":{targetX},\"tz\":{targetZ},\"distance\":{distance}}}";
        }

        return null;
    }
}
