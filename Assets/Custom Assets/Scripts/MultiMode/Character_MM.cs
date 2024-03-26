using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhantomBetrayal;
using UnityEngine.AI;

namespace PhantomBetrayal
{
    public class Character_MM : MonoBehaviour
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
            Idle, Moving,
            Atking, TakeAtking, HitGhostValid,
            BreakingBox, BreakBoxValid,
            MovingToTar_Com, SitProc_Com,
            Roll,
            Dead,
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- serialize fields
        [SerializeField] public CharacterData chaData = new CharacterData();

        [SerializeField] public CharacterValue chaValue = new CharacterValue();

        [SerializeField] GameObject magicEff_Pf;

        //-------------------------------------------------- public fields
        [ReadOnly] public List<GameState_En> gameStates = new List<GameState_En>();

        [ReadOnly] public List<ItemValue> itemValues = new List<ItemValue>();

        [ReadOnly] public Player_M player_Cp;

        [ReadOnly] public bool isTraitor;

        [ReadOnly] public Gem_M takenGem_Cp;

        //-------------------------------------------------- private fields
        // components
        Controller_Gp controller_Cp;
        PlayerManager pManager_Cp;
        EnvManager envManager_Cp;
        UIManager_Gp uiManager_Cp;
        MapManager mapManager_Cp;
        TraitorHandler traHandler_Cp;
        FinalArea finalArea_Cp;
        Finish finish_Cp;

        //
        float transInput, rotInput;

        float lastAtkTime;

        Quaternion prevRot;

        [SerializeField][ReadOnly] Vector3 destPos;

        [SerializeField][ReadOnly] Transform dest_Tf;

        [SerializeField][ReadOnly] Door_M enteredDoor_Cp;

        [SerializeField][ReadOnly] Box_M enteredBox_Cp;

        Ghost_M ghost_Cp;

        // ui
        [SerializeField][ReadOnly] PlayerIconArea pIconArea_Cp;

        [SerializeField][ReadOnly] InputActionPanel inputActionPanel_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Properties
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Properties

        //-------------------------------------------------- public properties
        public GameState_En mainGameState
        {
            get { return gameStates[0]; }
            set { gameStates[0] = value; }
        }

        public bool isCom { get { return pData.isCom; } }

        public bool isLocalPlayer { get { return pData.isLocalPlayer; } }

        public bool isAlive { get { return chaData.alive; } }

        public int chaId { get { return chaData.id; } }

        public bool hasGem { get { return takenGem_Cp != null ? true : false; } }

        //-------------------------------------------------- private properties
        PlayerData pData { get { return player_Cp.pData; } }

        CharactersData chsData { get { return pManager_Cp.chsData; } }

        NavMeshAgent agent_Cp { get { return chaValue.navAgent_Cp; } }

        Animator chaAnim_Cp { get { return chaValue.chatAnim_Cp; } }

        BoxData boxData { get { return envManager_Cp.boxData; } }

        GemData gemData { get { return envManager_Cp.gemData; } }

        #endregion

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////

        //-------------------------------------------------- Start is called before the first frame update
        void Start()
        {
            chaData.Init();

            prevRot = transform.rotation;
        }

        //-------------------------------------------------- Update is called once per frame
        void Update()
        {
            if (ExistGameStates(GameState_En.Playing))
            {
                if (!pData.isCom)
                {
                    if (isLocalPlayer)
                    {
                        Handle_Person();
                    }
                }
                else
                {
                    Handle_Com();
                }
            }
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
            AddMainGameState();

            SetComponents();

            InitVariables();

            mainGameState = GameState_En.Inited;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;

            pManager_Cp = controller_Cp.pManager_Cp;

            envManager_Cp = controller_Cp.envManager_Cp;

            player_Cp = gameObject.GetComponent<Player_M>();

            uiManager_Cp = controller_Cp.uiManager_Cp;

            mapManager_Cp = controller_Cp.mapManager_Cp;

            ghost_Cp = pManager_Cp.ghost_Cp;

            traHandler_Cp = controller_Cp.traitorHandler_Cp;

            finalArea_Cp = controller_Cp.finalArea_Cp;

            inputActionPanel_Cp = controller_Cp.inputActionPanel_Cp;

            finish_Cp = controller_Cp.finish_Cp;
        }

        //--------------------------------------------------
        void InitVariables()
        {
            // init agent_Cp
            agent_Cp.speed = chaData.norMoveSpd;
            agent_Cp.angularSpeed = chaData.rotSpd;
            agent_Cp.avoidancePriority += chaData.id;
            agent_Cp.stoppingDistance = 0.05f;

            // init ability UI
            SetHp(chaData.hp);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Play
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Play

        //--------------------------------------------------
        public void ReadyToPlay()
        {
            if (isLocalPlayer)
            {
                inputActionPanel_Cp.SetActiveChaInputPanel(true);
                inputActionPanel_Cp.EnableChaAtkBtn(false);
                inputActionPanel_Cp.EnableChaOpenBtn(false);
            }
        }

        //--------------------------------------------------
        public void Play()
        {
            AddGameStates(GameState_En.Playing);

            SetGold(chaData.gold);

            mainGameState = GameState_En.Idle;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle person
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle person

        //--------------------------------------------------
        public void Handle_Person()
        {
            Handle_Input();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle input
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle input

        //--------------------------------------------------
        void Handle_Input()
        {
            //
            float vertInput_tp = uiManager_Cp.vertInput;
            float horiInput_tp = uiManager_Cp.horInput;
            if (Mathf.Approximately(uiManager_Cp.vertInput, 0f) && Mathf.Approximately(uiManager_Cp.horInput, 0f))
            {
                vertInput_tp = Input.GetAxis("Vertical");
                horiInput_tp = Input.GetAxis("Horizontal");
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                DOTween.To(() => chsData.moveSpdCoef, x => chsData.moveSpdCoef = x,
                    chsData.maxMoveSpdCoef, chsData.spdCoefChangeDur);

                DOTween.To(() => chsData.rotSpdCoef, x => chsData.rotSpdCoef = x,
                    chsData.maxRotSpdCoef, chsData.spdCoefChangeDur);
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                DOTween.To(() => chsData.moveSpdCoef, x => chsData.moveSpdCoef = x,
                    chsData.minMoveSpdCoef, chsData.spdCoefChangeDur);

                DOTween.To(() => chsData.rotSpdCoef, x => chsData.rotSpdCoef = x,
                    chsData.minRotSpdCoef, chsData.spdCoefChangeDur);
            }

            DOTween.To(() => transInput, x => transInput = x, vertInput_tp,
                chsData.spdCoefChangeDur);
            DOTween.To(() => rotInput, x => rotInput = x, horiInput_tp,
                chsData.spdCoefChangeDur);

            Handle_Move();

            //
            if (Input.GetKeyDown(KeyCode.H))
            {
                Handle_Atk();
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                Handle_MoveDoor();
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                Handle_BreakBox();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Handle_Roll();
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle move
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle move

        //--------------------------------------------------
        void Handle_Move()
        {
            //
            if (mainGameState != GameState_En.Idle && mainGameState != GameState_En.Moving)
            {
                return;
            }

            //
            if (transInput != 0f || rotInput != 0f)
            {
                if (mainGameState == GameState_En.Idle)
                {
                    mainGameState = GameState_En.Moving;
                }
            }
            else
            {
                if (mainGameState == GameState_En.Moving)
                {
                    mainGameState = GameState_En.Idle;
                }
            }

            //
            Do_Action_Move();
        }

        //--------------------------------------------------
        void Do_Action_Move()
        {
            //
            chaValue.chatAnim_Cp.SetFloat("Walk", transInput * chsData.moveSpdCoef);

            chaValue.chatAnim_Cp.SetFloat("Turn", rotInput * chsData.rotSpdCoef);

            //
            transform.Translate(transInput * Vector3.forward * chaData.moveSpd * chsData.moveSpdCoef
                * Time.deltaTime);

            transform.Rotate(rotInput * Vector3.up * chaData.rotSpd * chsData.rotSpdCoef
                * Time.deltaTime);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle roll
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle roll

        //--------------------------------------------------
        public void Handle_Roll()
        {
            StartCoroutine(Corou_Handle_Roll());
        }

        IEnumerator Corou_Handle_Roll()
        {
            //
            if (mainGameState != GameState_En.Idle && mainGameState != GameState_En.Moving)
            {
                yield break;
            }

            if (!chaData.rollCdReseted)
            {
                yield break;
            }

            mainGameState = GameState_En.Roll;

            chaData.rollCdReseted = false;

            chaAnim_Cp.applyRootMotion = true;
            chaAnim_Cp.SetTrigger("Roll");

            yield return new WaitForSeconds(chaData.rollDur);

            chaAnim_Cp.applyRootMotion = false;

            mainGameState = GameState_En.Idle;

            yield return new WaitForSeconds(chaData.rollInterval - chaData.rollDur);
            chaData.rollCdReseted = true;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle attack
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle attack

        //--------------------------------------------------
        public void Handle_Atk()
        {
            StartCoroutine(Corou_Handle_Atk());
        }

        IEnumerator Corou_Handle_Atk()
        {
            //
            if (mainGameState != GameState_En.Idle && mainGameState != GameState_En.Moving)
            {
                yield break;
            }

            if (!chaValue.atkEnable)
            {
                yield break;
            }

            //
            mainGameState = GameState_En.Atking;

            //
            lastAtkTime = Time.time;

            Do_Action_Atk();
            yield return new WaitForSeconds(chaValue.hitValidStartTime);
            AddGameStates(GameState_En.HitGhostValid);

            yield return new WaitForSeconds(chaValue.hitValidEndTime - chaValue.hitValidStartTime);
            ExistGameStates(GameState_En.HitGhostValid);

            yield return new WaitForSeconds(chaValue.atkActionDur - chaValue.hitValidEndTime);

            //
            mainGameState = GameState_En.Idle;
        }

        //--------------------------------------------------
        void Do_Action_Atk()
        {
            chaValue.chatAnim_Cp.SetTrigger("Atk");
        }

        //--------------------------------------------------
        void OnWeaponCollideWithGhost(GameObject other_GO_pr)
        {
            if (!ExistGameStates(GameState_En.HitGhostValid))
            {
                return;
            }

            //ghost_Cp.Handle_TakeAtk(this);

            ShowHitEff(other_GO_pr);
        }

        //--------------------------------------------------
        void ShowHitEff(GameObject other_GO_pr)
        {
            StartCoroutine(Corou_ShowHitEff(other_GO_pr));
        }

        IEnumerator Corou_ShowHitEff(GameObject other_GO_pr)
        {
            GameObject eff_GO_tp = Instantiate(magicEff_Pf, other_GO_pr.transform);

            yield return new WaitForSeconds(1f);

            Destroy(eff_GO_tp);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle move door
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle move door

        //--------------------------------------------------
        public void Handle_MoveDoor(DoorOpenStatus openStatus = DoorOpenStatus.Toggle)
        {
            //
            if (mainGameState != GameState_En.Idle && mainGameState != GameState_En.Moving)
            {
                return;
            }

            if (enteredDoor_Cp == null)
            {
                return;
            }

            //
            Hash128 hash_tp = HashHandler.RegRandHash();
            if (openStatus == DoorOpenStatus.Toggle)
            {
                enteredDoor_Cp.MoveDoor(hash_tp);
            }
            else
            {
                enteredDoor_Cp.MoveDoor(openStatus, hash_tp);
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle break box
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle break box

        //--------------------------------------------------
        public void Handle_BreakBox()
        {
            StartCoroutine(Corou_Handle_BreakBox());
        }

        IEnumerator Corou_Handle_BreakBox()
        {
            //
            if (mainGameState != GameState_En.Idle && mainGameState != GameState_En.Moving)
            {
                yield break;
            }

            //
            mainGameState = GameState_En.BreakingBox;

            chaValue.chatAnim_Cp.SetTrigger("Break");

            yield return new WaitForSeconds(chaValue.breakStartTime);

            AddGameStates(GameState_En.BreakBoxValid);

            yield return new WaitForSeconds(chaValue.breakEndTime - chaValue.breakStartTime);

            RemoveGameStates(GameState_En.BreakBoxValid);

            mainGameState = GameState_En.Idle;
        }

        //--------------------------------------------------
        public bool IsValidBreakBox()
        {
            return (mainGameState == GameState_En.BreakingBox)
                && ExistGameStates(GameState_En.BreakBoxValid);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle taking attack
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle taking attack

        //--------------------------------------------------
        public void Handle_TakeAtk(Ghost_M atker_Cp_pr)
        {
            StartCoroutine(Corou_Handle_TakeAtk(atker_Cp_pr));
        }

        IEnumerator Corou_Handle_TakeAtk(Ghost_M atker_Cp_pr)
        {
            if (!ExistGameStates(GameState_En.Playing))
            {
                yield break;
            }

            TakeDmg(atker_Cp_pr);

            Do_Action_TakeAtk();
        }

        //--------------------------------------------------
        void TakeDmg(Ghost_M atker_Cp_pr)
        {
            if (isTraitor)
            {
                return;
            }

            ChangeHp(-atker_Cp_pr.dmg);

            if (chaData.hp == 0)
            {
                Handle_Death();
            }
        }

        //--------------------------------------------------
        void Do_Action_TakeAtk()
        {
            StartCoroutine(Corou_Do_Action_TakeAtk());
        }

        IEnumerator Corou_Do_Action_TakeAtk()
        {
            if (mainGameState != GameState_En.Idle && mainGameState != GameState_En.Moving)
            {
                yield break;
            }

            mainGameState = GameState_En.TakeAtking;

            chaValue.chatAnim_Cp.SetTrigger("TakeAtk");

            yield return new WaitForSeconds(chaValue.takeAtkActionDur);

            mainGameState = GameState_En.Moving;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle take item
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle take item

        //--------------------------------------------------
        public void TakeItem(Item_M item_Cp_pr)
        {
            StartCoroutine(Corou_TakeItem(item_Cp_pr));
        }

        IEnumerator Corou_TakeItem(Item_M item_Cp_pr)
        {
            item_Cp_pr.isTaken = true;

            //
            ItemValue itemValue_tp = item_Cp_pr.itemValue;
            itemValues.Add(itemValue_tp);
            Destroy(item_Cp_pr.gameObject);

            envManager_Cp.itemData.allItem_Cps.Remove(item_Cp_pr);

            //
            if (itemValue_tp.cont == ItemContent.Health)
            {
                ChangeHp(itemValue_tp.amount);

                GameObject eff_GO_tp = Instantiate(itemValue_tp.eff_Pf, chaValue.effTriPoint_middle_Tf);
                yield return new WaitForSeconds(itemValue_tp.effDur);
                Destroy(eff_GO_tp);
            }
            else if (itemValue_tp.cont == ItemContent.Gold)
            {
                ChangeGold(itemValue_tp.amount);

                GameObject eff_GO_tp = Instantiate(itemValue_tp.eff_Pf, chaValue.effTriPoint_bottom_Tf);
                yield return new WaitForSeconds(itemValue_tp.effDur);
                Destroy(eff_GO_tp);
            }
            else if (itemValue_tp.cont == ItemContent.Weapon)
            {


                GameObject weapon_GO_tp = Instantiate(itemValue_tp.eff_Pf, chaValue.hand_Tf);                
                yield return new WaitForSeconds(itemValue_tp.effDur);
                Destroy(weapon_GO_tp);
            }

            //
            itemValues.Remove(itemValue_tp);
        }

        //--------------------------------------------------
        public bool IsValidTakeItem()
        {
            return mainGameState != GameState_En.BreakingBox;
        }

        //--------------------------------------------------
        public void EquipWeapon(CharacterWeapon_M weapon_Cp_pr)
        {
            StartCoroutine(Corou_EquipWeapon(weapon_Cp_pr));
        }

        IEnumerator Corou_EquipWeapon(CharacterWeapon_M weapon_Cp_pr)
        {
            if (!IsValidEquipWeapon())
            {
                yield break;
            }

            weapon_Cp_pr.transform.SetParent(chaValue.hand_Tf, false);
            weapon_Cp_pr.transform.localPosition = Vector3.zero;
            weapon_Cp_pr.isHitEnable = true;
            weapon_Cp_pr.onCollideWithGhostAction = OnWeaponCollideWithGhost;

            chaValue.atkEnable = true;
            if (isLocalPlayer)
            {
                inputActionPanel_Cp.EnableChaAtkBtn(true);
            }

            yield return new WaitForSeconds(weapon_Cp_pr.validDur);

            Destroy(weapon_Cp_pr.gameObject);
            chaValue.atkEnable = false;
            if (isLocalPlayer)
            {
                inputActionPanel_Cp.EnableChaAtkBtn(false);
            }
        }

        public bool IsValidEquipWeapon()
        {
            return chaValue.atkEnable ? false : true;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle take gem
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle take gem

        //--------------------------------------------------
        public void TakeGem(Gem_M gem_Cp_tp)
        {
            StartCoroutine(Corou_TakeGem(gem_Cp_tp));
        }

        IEnumerator Corou_TakeGem(Gem_M gem_Cp_tp)
        {
            //gem_Cp_tp.OnTakenByCha(this);
            takenGem_Cp = gem_Cp_tp;
            gemData.inBoxGem_Cps.Remove(gem_Cp_tp);
            gemData.takenGem_Cps.Add(gem_Cp_tp);

            // player icon area
            pIconArea_Cp.OnTakeGem();

            yield return null;
        }

        //--------------------------------------------------
        public bool IsValidTakeGem()
        {
            return takenGem_Cp == null ? true : false;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle ability
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle ability

        //--------------------------------------------------
        void ChangeHp(int amount)
        {
            int hp_tp = Mathf.Clamp(chaData.hp + amount, 0, chaData.norHp);

            SetHp(hp_tp);
        }

        //--------------------------------------------------
        void SetHp(int hp_pr)
        {
            chaData.hp = hp_pr;

            //
            if (isLocalPlayer)
            {
                uiManager_Cp.SetHp(hp_pr);
            }
        }

        //--------------------------------------------------
        void ChangeGold(int amount)
        {
            SetGold(Mathf.Clamp(chaData.gold + amount, 0, chaData.maxGold));            
        }

        //--------------------------------------------------
        void SetGold(int gold_tp)
        {
            chaData.gold = gold_tp;

            //
            if (isLocalPlayer)
            {
                uiManager_Cp.SetGold(chaData.gold);
            }
            pIconArea_Cp.SetGold(chaData.gold);
        }

        //--------------------------------------------------
        void SetAtkEnable(bool flag)
        {
            chaValue.atkEnable = flag;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle death
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle death

        //--------------------------------------------------
        public void Handle_Death()
        {
            StartCoroutine(Corou_Handle_Death());
        }

        IEnumerator Corou_Handle_Death()
        {
            RemoveGameStates(GameState_En.Playing);
            mainGameState = GameState_En.Dead;

            chaData.alive = false;
            chaAnim_Cp.SetTrigger("Die");

            agent_Cp.isStopped = true;
            uiManager_Cp.OnCharacterDead(chaData.id);
            mapManager_Cp.OnCharacterDead(chaData.id);
            traHandler_Cp.RefreshJudgePanel();

            yield return new WaitForSeconds(2f);

            agent_Cp.enabled = false;
            DisableAllComponents();

            if (takenGem_Cp)
            {
                //takenGem_Cp.OnDropped(this);
                takenGem_Cp = null;
            }

            // player icon area
            pIconArea_Cp.OnCharacterDead();
            pIconArea_Cp.OnDropGem();

            finish_Cp.OnCharacterDead();
        }

        //--------------------------------------------------
        void DisableAllComponents()
        {
            foreach (MeshRenderer meshRend_Cp in GetComponentsInChildren<MeshRenderer>())
            {
                meshRend_Cp.enabled = false;
            }

            foreach (SkinnedMeshRenderer meshRend_Cp in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                meshRend_Cp.enabled = false;
            }

            foreach (Collider coll_Cp in GetComponentsInChildren<Collider>())
            {
                coll_Cp.enabled = false;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle ui
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle ui

        //--------------------------------------------------
        public void Handle_UI()
        {

        }

        //--------------------------------------------------
        public void SetPlayerIconArea(PlayerIconArea pIconArea_Cp_tp)
        {
            pIconArea_Cp = pIconArea_Cp_tp;

            pIconArea_Cp.SetIcon(chaData.icon);
        }

        //--------------------------------------------------
        public void SetChatMsg(string text)
        {
            pIconArea_Cp.SetMessage(text);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle com
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle com

        //--------------------------------------------------
        public void Handle_Com()
        {
            StartCoroutine(Corou_Handle_Com());
        }

        IEnumerator Corou_Handle_Com()
        {
            SyncMoveAnim();

            if (mainGameState == GameState_En.Idle)
            {
                if (!ExistGameStates(GameState_En.SitProc_Com))
                {
                    SetDestination();
                }
            }
            else if (mainGameState == GameState_En.Moving)
            {
                if (!ExistGameStates(GameState_En.SitProc_Com))
                {
                    CheckDestArrive();
                }
            }

            yield return null;
        }

        //--------------------------------------------------
        void SyncMoveAnim()
        {
            //
            chaAnim_Cp.SetFloat("Walk", agent_Cp.velocity.magnitude / agent_Cp.speed * chaData.norMoveSpd);

            //
            float rotSpd_tp = Quaternion.Angle(prevRot, transform.rotation) / Time.deltaTime;
            chaAnim_Cp.SetFloat("Turn", rotSpd_tp / chaData.rotSpd);
            prevRot = transform.rotation;
        }

        //--------------------------------------------------
        void SetDestination()
        {
            if (hasGem)
            {
                destPos = finalArea_Cp.gemPointGroup_Tf.position;
                dest_Tf = finalArea_Cp.gemPointGroup_Tf;
                agent_Cp.SetDestination(destPos);
            }
            else
            {
                int randSpawnIndex = Random.Range(0, boxData.spawnPoint_Tfs.Count);
                Transform randBox_Tf = boxData.spawnPoint_Tfs[randSpawnIndex];

                destPos = randBox_Tf.position;
                dest_Tf = randBox_Tf;
                agent_Cp.SetDestination(randBox_Tf.position);
            }

            mainGameState = GameState_En.Moving;
        }

        //--------------------------------------------------
        void CheckDestArrive()
        {
            if (Mathf.Approximately(agent_Cp.remainingDistance, 0f))
            {
                if (mainGameState == GameState_En.Moving)
                {
                    mainGameState = GameState_En.Idle;
                }
            }
        }

        //--------------------------------------------------
        public void SitProc_Door()
        {
            StartCoroutine(Corou_SitProc_Door());
        }

        IEnumerator Corou_SitProc_Door()
        {
            //
            if (ExistGameStates(GameState_En.SitProc_Com))
            {
                yield break;
            }

            AddGameStates(GameState_En.SitProc_Com);

            Handle_MoveDoor(DoorOpenStatus.Open);
            yield return new WaitForSeconds(1f);

            agent_Cp.destination = destPos;

            RemoveGameStates(GameState_En.SitProc_Com);
        }

        void SetEnteredDoor(GameObject other_GO_pr = null)
        {
            if (other_GO_pr == null)
            {
                enteredDoor_Cp = null;

                if (isLocalPlayer)
                {
                    inputActionPanel_Cp.EnableChaOpenBtn(false);
                }
            }
            else
            {
                enteredDoor_Cp = other_GO_pr.GetComponent<Door_M>();

                if (isLocalPlayer)
                {
                    inputActionPanel_Cp.EnableChaOpenBtn(true);
                }
            }
        }

        //--------------------------------------------------
        void SitProc_Box(GameObject box_GO_pr)
        {
            StartCoroutine(Corou_SitProc_Box(box_GO_pr));
        }

        IEnumerator Corou_SitProc_Box(GameObject box_GO_pr)
        {
            if (ExistGameStates(GameState_En.SitProc_Com))
            {
                yield break;
            }

            transform.DODynamicLookAt(box_GO_pr.transform.position, 0.3f, AxisConstraint.Y);

            AddGameStates(GameState_En.SitProc_Com);

            Handle_BreakBox();

            agent_Cp.destination = destPos;

            RemoveGameStates(GameState_En.SitProc_Com);
        }

        //--------------------------------------------------
        void SitProc_Ghost()
        {
            
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events from external
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events from external

        //--------------------------------------------------
        public void OnDropGemToFinalArea()
        {
            pIconArea_Cp.OnDropGem();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events

        //--------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (!ExistGameStates(GameState_En.Playing))
            {
                return;
            }

            if (other.CompareTag("Door"))
            {
                SetEnteredDoor(other.gameObject);
            }

            if (isCom)
            {
                if (other.CompareTag("Door"))
                {
                    SitProc_Door();
                }
                else if (other.CompareTag("BoxArea"))
                {
                    SitProc_Box(other.gameObject);
                }
                else if (other.CompareTag("Ghost"))
                {
                    SitProc_Ghost();
                }
            }
        }

        //--------------------------------------------------
        private void OnTriggerExit(Collider other)
        {
            if (!ExistGameStates(GameState_En.Playing))
            {
                return;
            }

            if (other.CompareTag("Door"))
            {
                SetEnteredDoor(null);
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Finish
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Finish

        //--------------------------------------------------
        public void OnFinish(bool isVictory_tp)
        {

        }

        #endregion
    }
}