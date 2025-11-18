using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GlobalStatLogger : MonoBehaviour // inspired from -- notioncheck --
{
    private const string SheetURL =
        "https://script.google.com/macros/s/AKfycbxNQ6sRaXTuyKVlJGU0O6-H8JdXrGZ4tszEBzSas04DNF1Asp7dALy-_OJ_DzQa98bKxw/exec";

    public static IEnumerator SendToGoogleSheet(Dictionary<string, object> data)
    {
        string json = JsonUtility.ToJson(new SerializableStats(data));
        using (UnityWebRequest www = new UnityWebRequest(SheetURL, "POST"))
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

    [System.Serializable]
    private class SerializableStats
    {
        public string player_id;
        public float time;
        public string game_state;
        public string wave_name;
        public int total_deaths;
        public int total_parries;
        public int total_dodges;
        public int total_completions;

        public SerializableStats(Dictionary<string, object> d)
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