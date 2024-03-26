using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PhantomBetrayal
{
    public class Ghost_M : MonoBehaviour
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
            Moving,
            Atking, ValidHitPeriod,
            TakeAtking, WillHit,
            BreakingBox, BreakBoxValid,
            SitProc_Com,
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- serialize fields
        [SerializeField] public int id;

        [SerializeField] NavMeshAgent agent_Cp;

        [SerializeField] Animator ghostAnim_Cp;

        [SerializeField] GhostWeapon_M weapon_Cp;

        [SerializeField] TraitorHandler traitorHandler_Cp;

        [SerializeField] public bool isCom;

        [SerializeField] public int hp = 100, maxHp = 100, dmg = 5;

        [SerializeField] public float moveSpd = 1.5f, rotSpd = 120f;

        [SerializeField] float stopDist = 1.5f, atkRange = 2f;

        [SerializeField] float atkInterval = 2f, atkActionDur = 1.47f;

        [SerializeField] float validHitStartTime = 0.54f, validHitEndTime = 1f;

        [SerializeField] GameObject hitEff_Pf;

        //-------------------------------------------------- public fields
        [ReadOnly]
        public List<GameState_En> gameStates = new List<GameState_En>();

        [ReadOnly] public List<int> detectedChas = new List<int>();

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;

        PlayerManager pManager_Cp;

        EnvManager envManager_Cp;

        MapManager mapManager_Cp;

        List<Character_M> cha_Cps = new List<Character_M>();

        [SerializeField][ReadOnly] Player_M player_Cp;

        [SerializeField][ReadOnly] Transform target_Tf;

        [SerializeField][ReadOnly] Door_M enteredDoor_Cp;

        float lastAtkTime;

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

        public bool isLocalPlayer { get { return player_Cp.isLocalPlayer; } }

        public bool isMoving
        {
            get { return ExistGameStates(GameState_En.Moving) ? true : false; }
        }

        public bool isPossMove
        {
            get
            {
                bool value = false;

                if (!isTakeAtking)
                {
                    value = true;
                }

                return value;
            }
        }

        public bool isAtking
        {
            get { return ExistGameStates(GameState_En.Atking) ? true : false; }
        }

        public bool isValidAtkTime
        {
            get
            {
                bool value = false;

                if (Time.time - lastAtkTime >= atkInterval)
                {
                    if (!isAtking)
                    {
                        value = true;
                    }
                }

                return value;
            }
        }

        public bool isTakeAtking
        {
            get { return ExistGameStates(GameState_En.TakeAtking) ? true : false; }
        }

        //-------------------------------------------------- private properties
        BoxData boxData { get { return envManager_Cp.boxData; } }

        bool isValidAtkRange
        {
            get
            {
                return (Vector3.Distance(target_Tf.position, transform.position) < atkRange);
            }
        }

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

        }

        //-------------------------------------------------- Update is called once per frame
        void Update()
        {
            if (ExistGameStates(GameState_En.Playing))
            {
                if (!isCom)
                {
                    Handle_Input();
                }
                else
                {
                    SetTarget();
                }

                Handle_Move();

                Handle_Atk();
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Manage gameStates
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region ManageGameStates

        //--------------------------------------------------
        public void AddMainGameState(GameState_En value)
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
            //
            AddMainGameState(GameState_En.Nothing);

            //
            SetComponents();

            InitVariables();

            //
            mainGameState = GameState_En.Inited;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
            pManager_Cp = controller_Cp.pManager_Cp;
            cha_Cps = pManager_Cp.cha_Cps;
            envManager_Cp = controller_Cp.envManager_Cp;
            mapManager_Cp = controller_Cp.mapManager_Cp;
            player_Cp = gameObject.GetComponent<Player_M>();
            traitorHandler_Cp = controller_Cp.traitorHandler_Cp;
        }

        //--------------------------------------------------
        void InitVariables()
        {
            //
            InitWeapon();

            //
            agent_Cp.speed = moveSpd;
            agent_Cp.angularSpeed = rotSpd;
            agent_Cp.stoppingDistance = stopDist;

            //
            lastAtkTime = -atkInterval;
        }

        //--------------------------------------------------
        void InitWeapon()
        {
            SetActive_Weapon(false);
            weapon_Cp.onCollideWithChaAction = OnCollideWithCha;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Play
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Play

        //--------------------------------------------------
        public void Play()
        {
            mainGameState = GameState_En.Playing;

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
            if (!isCom)
            {
                Handle_Move_Player();
            }
            else
            {
                Handle_Move_ComPlayer();
            }
        }

        //--------------------------------------------------
        void Handle_Move_Player()
        {

        }

        //--------------------------------------------------
        void Handle_Move_ComPlayer()
        {
            agent_Cp.SetDestination(target_Tf.position);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle target
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle target

        //--------------------------------------------------
        void SetTarget()
        {
            DetectCharacter();

            if (traitorHandler_Cp.isTraitorExit)
            {
                mapManager_Cp.OnTraitorAppear(traitorHandler_Cp.traitorCha_Cp.chaId);
            }
            else if (player_Cp.isLocalPlayer)
            {
                mapManager_Cp.OnCharacterDetectedByGho(detectedChas);
            }

            //
            if (target_Tf == null)
            {
                int randIndex = Random.Range(0, boxData.spawnPoint_Tfs.Count);
                target_Tf = boxData.spawnPoint_Tfs[randIndex];
            }

            CheckArriveToBox();
        }

        //--------------------------------------------------
        void DetectCharacter()
        {
            Transform tar_Tf_tp = null;

            detectedChas.Clear();

            float closestDistance = float.MaxValue;

            foreach (Character_M cha_Cp_tp in cha_Cps)
            {
                // check character is traitor
                if (cha_Cp_tp.isTraitor)
                {
                    continue;
                }
                if (!cha_Cp_tp.isAlive)
                {
                    continue;
                }

                // Calculate the direction from the current object to the character
                Vector3 directionToCharacter = cha_Cp_tp.transform.position - transform.position;

                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToCharacter, out hit))
                {
                    //
                    detectedChas.Add(cha_Cp_tp.chaId);

                    // Check if the character is placed at the front of the transform
                    if (Vector3.Dot(transform.forward, directionToCharacter.normalized) > 0)
                    {
                        // Check if the character is closer than the current closest one
                        if (hit.distance < closestDistance)
                        {
                            closestDistance = hit.distance;
                            tar_Tf_tp = cha_Cp_tp.transform;
                        }
                    }
                }
            }

            target_Tf = tar_Tf_tp;
        }

        //--------------------------------------------------
        void CheckArriveToBox()
        {
            if (target_Tf == null)
            {
                return;
            }

            if (PlayerManager.GetPlayerType(target_Tf.gameObject) == PlayerType.Null)
            {
                if (Mathf.Approximately(agent_Cp.remainingDistance, stopDist))
                {
                    target_Tf = null;
                }
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle attack
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle attack

        //--------------------------------------------------
        void Handle_Atk()
        {
            if (!isCom)
            {
                Handle_Atk_Player();
            }
            else
            {
                Handle_Atk_ComPlayer();
            }
        }

        //--------------------------------------------------
        void Handle_Atk_Player()
        {
            StartCoroutine(Corou_Handle_Atk());
        }

        IEnumerator Corou_Handle_Atk()
        {

            yield return null;
        }

        //--------------------------------------------------
        void Handle_Atk_ComPlayer()
        {
            StartCoroutine(Corou_Handle_Atk_ComPlayer());
        }

        IEnumerator Corou_Handle_Atk_ComPlayer()
        {
            if (!isValidAtkTime || !isValidAtkRange)
            {
                yield break;
            }

            if (ExistGameStates(GameState_En.Atking))
            {
                yield break;
            }

            transform.DODynamicLookAt(target_Tf.position, 0.3f, AxisConstraint.Y);

            AddGameStates(GameState_En.Atking);

            lastAtkTime = Time.time;

            Do_Action_Atk();

            yield return new WaitForSeconds(validHitStartTime);
            AddGameStates(GameState_En.ValidHitPeriod);
            SetActive_Weapon(true);

            yield return new WaitForSeconds(validHitEndTime - validHitStartTime);
            SetActive_Weapon(false);
            RemoveGameStates(GameState_En.ValidHitPeriod);

            yield return new WaitForSeconds(atkActionDur - validHitEndTime);
            RemoveGameStates(GameState_En.Atking);

            if (atkInterval > atkActionDur)
            {
                yield return new WaitForSeconds(atkInterval - atkActionDur);
            }
        }

        //--------------------------------------------------
        void Do_Action_Atk()
        {
            ghostAnim_Cp.SetTrigger("Atk");
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle take atk
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region

        //--------------------------------------------------
        public void Handle_TakeAtk(Character_M cha_Cp_pr)
        {
            int dmg = cha_Cp_pr.chaData.dmg;

            TakeDmg(dmg);
        }

        //--------------------------------------------------
        void TakeDmg(int dmg)
        {
            int hp_tp = Mathf.Clamp(hp - dmg, 0, maxHp);

            SetHp(hp_tp);
        }

        //--------------------------------------------------
        void SetHp(int hp_pr)
        {
            hp = hp_pr;
        }

        #endregion
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle move door
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle move door

        //--------------------------------------------------
        void Handle_MoveDoor(DoorOpenStatus openStatus = DoorOpenStatus.Toggle)
        {
            //
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
        /// Situation proceed
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Situation proceed

        //--------------------------------------------------
        void SitProc_Door()
        {
            StartCoroutine(Corou_SitProc_Door());
        }

        IEnumerator Corou_SitProc_Door()
        {
            if (ExistGameStates(GameState_En.SitProc_Com))
            {
                yield break;
            }

            AddGameStates(GameState_En.SitProc_Com);

            Handle_MoveDoor(DoorOpenStatus.Open);
            yield return new WaitForSeconds(1f);

            RemoveGameStates(GameState_En.SitProc_Com);
        }

        //--------------------------------------------------
        void SetEnteredDoor(GameObject other_GO_pr = null)
        {
            if (other_GO_pr == null)
            {
                enteredDoor_Cp = null;
            }
            else
            {
                enteredDoor_Cp = other_GO_pr.GetComponent<Door_M>();
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Custom callback methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Custom callback methods

        //--------------------------------------------------
        public void OnCollideWithCha(GameObject other_GO_pr)
        {
            if (!ExistGameStates(GameState_En.ValidHitPeriod))
            {
                return;
            }

            pManager_Cp.HandleFight(this, other_GO_pr.transform.root.GetComponent<Character_M>());

            AddHitEff(other_GO_pr);
        }

        //--------------------------------------------------
        void AddHitEff(GameObject other_GO_pr)
        {
            StartCoroutine(Corou_AddHitEff(other_GO_pr));
        }

        IEnumerator Corou_AddHitEff(GameObject other_GO_pr)
        {
            GameObject hitEff_GO_tp = Instantiate(hitEff_Pf, other_GO_pr.transform);
            yield return new WaitForSeconds(1f);
            Destroy(hitEff_GO_tp);
        }

        //--------------------------------------------------
        public void SetActive_Weapon(bool flag)
        {
            weapon_Cp.gameObject.SetActive(flag);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Functionality methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Functionality methods

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Default events
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Default events

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
                SetEnteredDoor();
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