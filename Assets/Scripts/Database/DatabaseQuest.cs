using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DatabaseQuest
{
    public int quest_id;
    public string quest_name;
    public string description;
    public string quest_type; // main, side, daily
    public int min_level;
    public int reward_gold;
    public int reward_exp;
    public int reward_item_id;
    public bool is_repeatable;
    public string difficulty; // easy, normal, hard
    public string created_at;
}

[System.Serializable]
public class DatabaseQuestObjective
{
    public int objective_id;
    public int quest_id;
    public string objective_type; // collect, kill, talk, reach
    public int target_id;
    public string target_name;
    public int quantity;
    public string description;
}

[System.Serializable]
public class DatabasePlayerQuest
{
    public int player_quest_id;
    public int player_id;
    public int quest_id;
    public string status; // not_started, in_progress, completed, failed
    public string accepted_at;
    public string completed_at;
}

[System.Serializable]
public class DatabaseQuestProgress
{
    public int player_id;
    public int quest_id;
    public int objective_id;
    public int current_count;
}

[System.Serializable]
public class DatabaseNPCQuest
{
    public int npc_id;
    public int quest_id;
}

[System.Serializable]
public class QuestWithDetails
{
    public DatabaseQuest quest;
    public List<DatabaseQuestObjective> objectives;
    public DatabasePlayerQuest playerQuest;
    public List<DatabaseQuestProgress> progress;
}

[System.Serializable]
public class DatabaseQuestList
{
    public List<DatabaseQuest> quests;
}

[System.Serializable]
public class DatabaseQuestObjectiveList
{
    public List<DatabaseQuestObjective> objectives;
}

[System.Serializable]
public class DatabasePlayerQuestList
{
    public List<DatabasePlayerQuest> player_quests;
}

[System.Serializable]
public class DatabaseQuestProgressList
{
    public List<DatabaseQuestProgress> progress;
}

[System.Serializable]
public class DatabaseNPCQuestList
{
    public List<DatabaseNPCQuest> npc_quests;
}
