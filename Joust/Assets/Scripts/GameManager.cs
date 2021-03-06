﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using NDream.AirConsole;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class GameManager : MonoBehaviour
{
    // singleton instance of the GameManager
    public static GameManager instance = null;

    // message types used for communicating with airconsole
    private const string TEAM_MESSAGE = "team";
    private const string LOCK_MESSAGE = "lock";
    private const string COUNTDOWN_MESSAGE = "countdown";
    private const string START_MESSAGE = "start";

    private bool gameStarting = false;
    private List<Team> Teams = new List<Team>();
    private List<HumanPlayer> Players = new List<HumanPlayer>();

    public int numTeams = 2;
    public int countdownTime = 3;
    public Text countdownText;
    public GameObject playerObject;

    public void StartGame()
    {
        // set the controllers to game mode by signaling the game has started
        AirConsole.instance.Broadcast(new Message(START_MESSAGE));

        // load the main scene
        SceneManager.LoadScene("MainScene");
    }

    // Unity method for initialization
    private void Awake()
    {
        // manage the singleton instance of the GameManager
        if (instance == null)
            instance = this;
        else if (instance != null)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        // create teams. there should be at least two
        this.Teams = new List<Team>
        {
            new Team(0, "blue"),
            new Team(1, "green")
        };

        // if the number of teams is set to three (or higher), add a third team. 
        if (numTeams >= 3)
        {
            this.Teams.Add(new Team(2, "white"));
        }

        // set the menu text
        this.countdownText.text = "";

        // register airconsole events
        AirConsole.instance.onConnect += AirConsole_onConnect;
        AirConsole.instance.onDisconnect += AirConsole_onDisconnect;
        AirConsole.instance.onCustomDeviceStateChange += AirConsole_onCustomDeviceStateChange;
        AirConsole.instance.onMessage += AirConsole_onMessage;
    }

    private void OnLevelWasLoaded(int level)
    {
        //TODO: How to associate level number with scene name?
        // level 1 is currently main scene
        if (level == 1)
        {
            // instantiate a bee for each player
            foreach(var team in this.Teams)
            {
                var teamNumber = team.Number;
                foreach(var player in team.Players)
                {
                    player.Bee = Instantiate(playerObject, new Vector3((int)Mathf.Pow(-1, teamNumber), -1), Quaternion.identity) as GameObject;

                    var beeScript = player.Bee.GetComponent<BeeScript>();
                    beeScript.TeamNumber = teamNumber;
                    beeScript.PlayerNumber = player.Number;
                }
            }
        }
    }

    private void UpdatePlayerCount()
    {
        if (!gameStarting)
        {
            var numPlayers = this.Players.Count;
            var numPlayersText = GameObject.Find("NumPlayersText").GetComponent<Text>();
            numPlayersText.text = "# Players: " + numPlayers;

            var numReady = this.Players.Where(p => p.IsReady).Count();
            var numReadyText = GameObject.Find("NumReadyText").GetComponent<Text>();
            numReadyText.text = "# Ready Players: " + numReady;

            // if all the players are ready, begin counting down to the start of the game
            if (numPlayers == numReady)
            {
                this.gameStarting = true;
                StartCoroutine(CountdownToStart(this.countdownTime));
            }
        }
    }

    private IEnumerator CountdownToStart(int seconds)
    {
        // lock the controllers to keep people from toggling their ready state
        AirConsole.instance.Broadcast(new Message(LOCK_MESSAGE));

        // countdown to 0, pausing every second
        for (int i = seconds; i > 0; i--)
        {
            // update the menu text and controllers with the time remaining until start
            this.countdownText.text = "Starting in " + i;
            AirConsole.instance.Broadcast(new Message<int>(COUNTDOWN_MESSAGE, i));

            yield return new WaitForSeconds(1);
        }

        this.countdownText.text = "GO!";
        this.StartGame();
    }

    private void AssignToTeam(HumanPlayer player)
    {
        var leastTeamMembers = this.Teams.Select(t => t.Players.Count).Min();
        var smallestTeam = this.Teams.First(t => t.Players.Count == leastTeamMembers);

        smallestTeam.Players.Add(player);

        AirConsole.instance.Message(player.DeviceId, new Message<string>(TEAM_MESSAGE, smallestTeam.Color));
    }

    private void AirConsole_onConnect(int device_id)
    {
        var player = new HumanPlayer(device_id);

        this.Players.Add(player);
        this.AssignToTeam(player);

        // give the player the smallest number not yet assigned
        player.Number = this.GetNextNumber(this.Players.Select(p => p.Number));

        this.UpdatePlayerCount();
    }

    private void AirConsole_onDisconnect(int device_id)
    {
        var player = this.Players.FirstOrDefault(p => p.DeviceId == device_id);
        if (player != null)
        {
            var playerTeam = this.Teams.FirstOrDefault(t => t.Players.Contains(player));
            if (playerTeam != null)
            {
                playerTeam.Players.Remove(player);
            }

            this.Players.Remove(player);
        }

        this.UpdatePlayerCount();
    }

    private void AirConsole_onCustomDeviceStateChange(int device_id, JToken custom_device_data)
    {
        Debug.Log("State changed");

        if (custom_device_data["isPlayerReady"] != null)
        {
            var player = this.Players.FirstOrDefault(p => p.DeviceId == device_id);
            if (player != null)
            {
                bool isReady;
                if (bool.TryParse(custom_device_data["isPlayerReady"].ToString(), out isReady))
                {
                    player.IsReady = isReady;
                }
            }

            this.UpdatePlayerCount();
        }
    }

    private void AirConsole_onMessage(int from, JToken data)
    {
        Debug.Log(string.Format("Message received from device id {0}: {1}", from, data));

        var player = this.Players.FirstOrDefault(p => p.DeviceId == from);
        if (player != null && player.Bee != null)
        {
            //TODO: we shouldn't be referencing the bee script, this should be hidden within the bee class
            var beeScript = player.Bee.GetComponent<BeeScript>();


            bool left = data["left"] != null ? (bool)data["left"] : false;
            bool right = data["right"] != null ? (bool)data["right"] : false;
            bool up = data["up"] != null ? (bool)data["up"] : false;

            int horizontal = 0;
            if (left && !right)
                horizontal = -1;
            else if (!left && right)
                horizontal = 1;

            beeScript.MoveBee(horizontal, up);
        }
    }

    private int GetNextNumber(IEnumerable<int> numbers)
    {
        if (numbers != null)
        {
            var numberList = numbers.ToList();

            if (numberList.Any())
            {
                var missingNumbers = Enumerable.Range(1, numberList.Max()).Except(numberList).ToList();

                if (missingNumbers.Any())
                {
                    return missingNumbers.Min();
                }

                return numberList.Max() + 1;
            }
        }

        return 1;
    }
}
