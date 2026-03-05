using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour
{
    public string teamName;
    public Color color;
    public int ShipsTotal;
    public int ShipsLost;
    public int ShipsKilled;

    public List<Ship> Ships = new List<Ship>();
    public List<Ship> Threats = new List<Ship>();
    public List<Team> teams = new List<Team>();


    public bool Friend(Ship other)
    {
        //only returns true if they're on another team, not on null team
        if (other && other.team == this) return true;
        return false;
    }

    public bool Foe(Ship other)
    {
        //for now anything on any other team is a foe
        //need teams to recognize allied teams
        if (other && other.team && other.team != this) return true;
        return false;
    }



    private void OnEnable()
    {
        UpdateTeamList();

    }

    void Update()
    {
        ShipsTotal = Ships.Count;
        UpdateThreatList();
    }

    private void UpdateTeamList()
    {
        teams.Clear();
        foreach (Team team in FindObjectsByType<Team>(FindObjectsSortMode.None))
        {
            if (team != this) teams.Add(team);
        }
    }

    private void UpdateThreatList()
    {
        Threats.Clear();
        foreach (Team team in teams)
        {
            Threats.AddRange(team.Ships);
        }
    }

}
