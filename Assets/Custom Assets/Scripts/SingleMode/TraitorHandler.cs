using Michsky.UI.Shift;
using PhantomBetrayal;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TraitorHandler : MonoBehaviour
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Types
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Types

    public enum GameState_En
    {
        Nothing, Inited, Playing,
        Judging,
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Fields
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Fields

    //-------------------------------------------------- serialize fields
    [SerializeField] float genTraitorMinTime = 3f, genTraitorMaxTime = 5f;

    [SerializeField] Text traitorNewsText_Cp;

    [SerializeField] Animator traitorNewsAnim_Cp;

    [SerializeField] float judgeInterval = 60f;

    [SerializeField] float judgeDur = 30f;

    [SerializeField] Transform judgePanel_Tf;

    [SerializeField] Transform selectTraPanel_Tf;

    [SerializeField] GameObject selectTraBtn_Pf;

    [SerializeField] Button judgePanelShowBtn_Cp;

    //-------------------------------------------------- public fields
    [ReadOnly] public List<GameState_En> gameStates = new List<GameState_En>();

    [ReadOnly] public Character_M traitorCha_Cp;

    [ReadOnly] public Traitor_M traitor_Cp;

    [ReadOnly] public Ghost_M ghost_Cp;

    [ReadOnly] public bool isTraitorExit;

    [ReadOnly] public Character_M traitorCand_Cp;

    //-------------------------------------------------- private fields
    Controller_Gp controller_Cp;

    PlayerManager pManager_Cp;

    Animator judgePanelAnim_Cp;

    Character_M localCha_Cp;

    Dictionary<int, int> traitorCandIds = new Dictionary<int, int>();

    [SerializeField][ReadOnly] List<Button> selectTraBtn_Cps = new List<Button>();

    [SerializeField][ReadOnly] List<SwitchManager> selectTraSwitch_Cps = new List<SwitchManager>();

    bool judgeCdReseted = true;

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Properties
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Properties

    //--------------------------------------------------
    public static TraitorHandler instance;

    //-------------------------------------------------- public properties
    public GameState_En mainGameState
    {
        get { return gameStates[0]; }
        set { gameStates[0] = value; }
    }

    public int traitorChaId { get { return traitorCha_Cp.chaId; } }

    //-------------------------------------------------- private properties
    List<Character_M> cha_Cps { get { return pManager_Cp.cha_Cps; } }

    int aliveChaCount { get { return pManager_Cp.aliveChaCount; } }

    #endregion

    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Methods
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////

    //--------------------------------------------------
    private void Awake()
    {
        instance = this;
    }

    //-------------------------------------------------- Start is called before the first frame update
    void Start()
    {

    }

    //-------------------------------------------------- Update is called once per frame
    void Update()
    {

    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Manage gameStates
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region ManageGameStates

    //--------------------------------------------------
    public void AddMainGameState(GameState_En value = GameState_En.Nothing)
    {
        if (gameStates.Count == 0)
        {
            gameStates.Add(value);
        }
    }

    //--------------------------------------------------
    public void AddGameStates(params GameState_En[] values)
    {
        foreach (GameState_En value_tp in values)
        {
            gameStates.Add(value_tp);
        }
    }

    //--------------------------------------------------
    public bool ExistGameStates(params GameState_En[] values)
    {
        bool result = true;
        foreach (GameState_En value in values)
        {
            if (!gameStates.Contains(value))
            {
                result = false;
                break;
            }
        }

        return result;
    }

    //--------------------------------------------------
    public bool ExistAnyGameStates(params GameState_En[] values)
    {
        bool result = false;
        foreach (GameState_En value in values)
        {
            if (gameStates.Contains(value))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    //--------------------------------------------------
    public int GetExistGameStatesCount(GameState_En value)
    {
        int result = 0;

        for (int i = 0; i < gameStates.Count; i++)
        {
            if (gameStates[i] == value)
            {
                result++;
            }
        }

        return result;
    }

    //--------------------------------------------------
    public void RemoveGameStates(params GameState_En[] values)
    {
        foreach (GameState_En value in values)
        {
            gameStates.RemoveAll(gameState_tp => gameState_tp == value);
        }
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Initialize
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Initialize

    //--------------------------------------------------
    public void Init()
    {
        AddMainGameState(GameState_En.Nothing);

        SetComponents();

        InitSelectTraitorButtons();

        InitVariables();

        RefreshJudgePanel();

        mainGameState = GameState_En.Inited;
    }

    //--------------------------------------------------
    void SetComponents()
    {
        controller_Cp = Controller_Gp.instance;
        pManager_Cp = controller_Cp.pManager_Cp;
        ghost_Cp = pManager_Cp.ghost_Cp;
    }

    //--------------------------------------------------
    void InitSelectTraitorButtons()
    {
        int playerCount = pManager_Cp.cha_Cps.Count;

        for (int i = 0; i < playerCount; i++)
        {
            GameObject selectTra_GO_tp = Instantiate(selectTraBtn_Pf, selectTraPanel_Tf);
            selectTraBtn_Cps.Add(selectTra_GO_tp.GetComponent<Button>());
            SwitchManager switch_Cp = selectTra_GO_tp.GetComponentInChildren<SwitchManager>();
            selectTraSwitch_Cps.Add(switch_Cp);

            int i_tp = i;
            switch_Cp.OnEvents.AddListener(() => SelectSwitch(i_tp));
        }
    }

    //--------------------------------------------------
    void InitVariables()
    {
        localCha_Cp = pManager_Cp.localCha_Cp;

        judgePanelAnim_Cp = judgePanel_Tf.GetComponent<Animator>();
    }

    #endregion

    //--------------------------------------------------
    public void GenTraitorRandomly()
    {
        StartCoroutine(Corou_GenTraitorRandomly());
    }

    IEnumerator Corou_GenTraitorRandomly()
    {
        float genTime = Random.Range(genTraitorMinTime, genTraitorMaxTime);
        yield return new WaitForSeconds(genTime);

        //
        int randChaIndex = Random.Range(0, cha_Cps.Count);
        traitorCha_Cp = cha_Cps[randChaIndex];
        traitor_Cp = traitorCha_Cp.AddComponent<Traitor_M>();
        traitor_Cp.Init();

        isTraitorExit = true;

        if (traitorCha_Cp.isLocalPlayer)
        {
            SetTraitorNews("あなたは裏切り者に変えられました。");
        }
        else if (ghost_Cp.isLocalPlayer)
        {
            SetTraitorNews("裏切り者が現れた。");
        }

        //
        pManager_Cp.OnTraitorAppear();
    }

    //--------------------------------------------------
    public void SetTraitorNews(string text)
    {
        traitorNewsText_Cp.text = text;
        traitorNewsAnim_Cp.SetTrigger("play");
    }

    //--------------------------------------------------
    void ShowTraitorCandText(Character_M cha_Cp_tp)
    {
        
    }

    //--------------------------------------------------
    void KillTraitor(Character_M cha_Cp_tp)
    {
        cha_Cp_tp.Handle_Death();
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Handle judge panel
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Handle judge panel

    //--------------------------------------------------
    public void ApplyJudge()
    {
        StartCoroutine(Corou_ApplyJudge());
    }

    IEnumerator Corou_ApplyJudge()
    {
        if (ExistGameStates(GameState_En.Judging))
        {
            yield break;
        }
        if (!judgeCdReseted)
        {
            yield break;
        }

        AddGameStates(GameState_En.Judging);
        judgeCdReseted = false;
        judgePanelShowBtn_Cp.interactable = false;

        if (pManager_Cp.isServer)
        {
            foreach (Character_M cha_Cp_tp in cha_Cps)
            {
                if (cha_Cp_tp.isCom)
                {
                    HandleJudge_Com(cha_Cp_tp);
                }
            }
        }

        RefreshJudgePanel();
        judgePanelAnim_Cp.SetTrigger("open");
        Invoke("InvokeJudgeCooldown", judgeInterval);
                
        Invoke("CancelJudge", judgeDur);
    }

    void InvokeJudgeCooldown()
    {
        judgeCdReseted = true;
        judgePanelShowBtn_Cp.interactable = true;
    }

    void CancelJudge()
    {
        CancelInvoke("CancelJudge");
        judgePanelAnim_Cp.SetTrigger("close");
        RemoveGameStates(GameState_En.Judging);
    }

    //--------------------------------------------------
    public void RefreshJudgePanel()
    {
        //
        for (int i = 0; i < selectTraSwitch_Cps.Count; i++)
        {
            selectTraBtn_Cps[i].GetComponentInChildren<TextMeshProUGUI>().text = cha_Cps[i].chaData.name;
        }

        //
        for (int i = 0; i < selectTraBtn_Cps.Count; i++)
        {
            if (cha_Cps[i].player_Cp.isLocalPlayer || !cha_Cps[i].isAlive)
            {
                selectTraBtn_Cps[i].interactable = false;
                selectTraSwitch_Cps[i].gameObject.SetActive(false);
            }
        }

        //
        SelectSwitch();
    }

    //--------------------------------------------------
    public void SelectSwitch(int index = -1)
    {
        for (int i = 0; i < selectTraSwitch_Cps.Count; i++)
        {
            if (i != index)
            {
                selectTraSwitch_Cps[i].isOn = false;
                selectTraSwitch_Cps[i].switchAnimator.Play("Switch Off");
            }
            else
            {
                selectTraSwitch_Cps[i].isOn = true;
                selectTraSwitch_Cps[i].switchAnimator.Play("Switch On");
            }
        }
    }

    //--------------------------------------------------
    void HandleJudge_Com(Character_M selfCha_Cp_tp)
    {
        SetTraitorCand_Com(selfCha_Cp_tp);
    }

    //--------------------------------------------------
    void SetTraitorCand_Com(Character_M selfCha_Cp_tp)
    {
        if (aliveChaCount <= 2)
        {
            return;
        }

        int randTraitorCandIndex_tp = 0;
        while (true)
        {
            randTraitorCandIndex_tp = Random.Range(0, cha_Cps.Count);

            Character_M otherCha_Cp_tp = cha_Cps[randTraitorCandIndex_tp];
            if (selfCha_Cp_tp != otherCha_Cp_tp)
            {
                if (otherCha_Cp_tp.chaData.alive)
                {
                    break;
                }
            }
        }

        traitorCandIds[selfCha_Cp_tp.chaId] = cha_Cps[randTraitorCandIndex_tp].chaId;
    }

    //--------------------------------------------------
    void SetTraitorCand()
    {
        //
        if (aliveChaCount <= 2)
        {
            return;
        }
        if (traitorCandIds.Count != aliveChaCount)
        {
            return;
        }

        // evaluate
        int traitorCandId_tp = -1;
        foreach (int i in traitorCandIds.Values)
        {
            int iCount = 0;
            foreach (int j in traitorCandIds.Values)
            {
                if (j == i)
                {
                    iCount++;
                }
            }
            if (iCount == (traitorCandIds.Count - 1))
            {
                traitorCandId_tp = i;
                break;
            }
        }
        if (traitorCandId_tp == -1)
        {
            return;
        }

        // set traitor cand
        ShowTraitorCandText(pManager_Cp.GetChaFromId(traitorCandId_tp));
        KillTraitor(pManager_Cp.GetChaFromId(traitorCandId_tp));

        // reset traitorCandIds
        traitorCandIds.Clear();
    }

    //--------------------------------------------------
    public void OnEnterTraitorCandidate()
    {
        traitorCandIds[localCha_Cp.chaId] = -1;
        //
        for (int i = 0; i < selectTraSwitch_Cps.Count; i++)
        {
            if (selectTraSwitch_Cps[i].isOn)
            {
                traitorCandIds[localCha_Cp.chaId] = cha_Cps[i].chaId;
                break;
            }
        }

        //
        CancelJudge();
        SetTraitorCand();
    }

    //--------------------------------------------------
    public void OnCancelTraitorCandidate()
    {
        //
        for (int i = 0; i < selectTraSwitch_Cps.Count; i++)
        {
            if (selectTraSwitch_Cps[i].isOn)
            {
                traitorCandIds[localCha_Cp.chaId] = -1;
                break;
            }
        }

        //
        CancelJudge();
        SetTraitorCand();
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Finish
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Finish

    //--------------------------------------------------
    public void OnFinish()
    {

    }

    #endregion
}
