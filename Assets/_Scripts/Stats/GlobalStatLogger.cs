using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GlobalStatLogger : MonoBehaviour 
{   // inspired from "Unity to Google Spreadsheet - Sending Data the Easy Way" -- https://www.youtube.com/watch?v=WM7f4yN4ZHA --
    private const string SheetURL =
        "https://script.google.com/macros/s/AKfycbyjq_SItdFj1FPUAeS_aaH0GhwesN0nAxVzSkqiYngC1XTDUDBWKQirmwL4z-hY6WS-Wg/exec";

    public static IEnumerator SendToGoogleSheet(Dictionary<string, object> data) // called from StatsManager
    {
        string json = JsonUtility.ToJson(new SerializableStats(data)); // converts serialized data to json to send as a web request
        using (UnityWebRequest www = new UnityWebRequest(SheetURL, "POST")) // POST request (send data to website)
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError(www.error);
        }
    }

    [Serializable]
    private class SerializableStats // stats need to be serialized to be ported using json
    {
        public string player_id; // explicit fields that will become json keys
        public float time;
        public string game_state;
        public string wave_name;
        public int total_deaths;
        public int total_parries;
        public int total_dodges;
        public int total_completions;

        public SerializableStats(Dictionary<string, object> d) // constructs values into strings, floats or integers so JSON can process the data
        {
            player_id = d["player_id"].ToString();
            time = Convert.ToSingle(d["time"]);
            game_state = d["game_state"].ToString();
            wave_name = d["wave_name"].ToString();
            total_deaths = Convert.ToInt32(d["total_deaths"]);
            total_parries = Convert.ToInt32(d["total_parries"]);
            total_dodges = Convert.ToInt32(d["total_dodges"]);
            total_completions = Convert.ToInt32(d["total_completions"]);
        }
    }
}