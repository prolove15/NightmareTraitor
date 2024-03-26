using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using PhantomBetrayal;

namespace PhantomBetrayal
{
    public class EnvManager : MonoBehaviour
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
            GenItemEnabled,
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- static fields
        public static EnvManager instance;

        //-------------------------------------------------- serialize fields
        [SerializeField]
        public ItemData itemData = new ItemData();

        [SerializeField]
        public GemData gemData = new GemData();

        [SerializeField]
        public BoxData boxData = new BoxData();

        //-------------------------------------------------- public fields
        [ReadOnly]
        public List<GameState_En> gameStates = new List<GameState_En>();

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;

        DoorData doorData = new DoorData();

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

        //-------------------------------------------------- private properties

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
            StartCoroutine(Corou_Init());
        }

        IEnumerator Corou_Init()
        {
            AddMainGameState();

            SetComponents();

            SetDoorData();

            InitBoxData();

            mainGameState = GameState_En.Inited;
            yield return null;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
        }

        //--------------------------------------------------
        void SetDoorData()
        {
            List<Door_M> door_Cps_tp = FindObjectsOfType<Door_M>().ToList();

            doorData.data = door_Cps_tp;
        }

        //--------------------------------------------------
        void InitBoxData()
        {
            for (int i = 0; i < boxData.spawnGroup_Tf.childCount; i++)
            {
                boxData.spawnPoint_Tfs.Add(boxData.spawnGroup_Tf.GetChild(i));
            }
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
            Handle_BoxGeneration();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle box generation
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle box generation

        //--------------------------------------------------
        public void Handle_BoxGeneration()
        {
            StartCoroutine(Corou_Handle_BoxGeneration());
        }

        IEnumerator Corou_Handle_BoxGeneration()
        {
            AddGameStates(GameState_En.GenItemEnabled);

            while (true)
            {
                float randSpawnInter = Random.Range(boxData.spawnInterMin, boxData.spawnInterMax);
                yield return new WaitForSeconds(randSpawnInter);
                yield return new WaitUntil(() => ExistGameStates(GameState_En.GenItemEnabled));

                GenerateBox();
            }
        }

        //--------------------------------------------------
        void GenerateBox()
        {
            // set random position index
            int randPosIndex = 0;
            if (boxData.box_GOs.Count == boxData.spawnPoint_Tfs.Count)
            {
                return;
            }
            else
            {
                do
                {
                    randPosIndex = Random.Range(0, boxData.spawnPoint_Tfs.Count);
                }
                while (boxData.box_GOs.ContainsKey(randPosIndex));
            }

            // instant box
            Box_M box_Cp_tp = Instantiate(boxData.box_Pf, boxData.spawnPoint_Tfs[randPosIndex])
                .GetComponent<Box_M>();
            boxData.box_GOs.Add(randPosIndex, box_Cp_tp.gameObject);
            box_Cp_tp.onBreakAction = OnBreakBoxAction;
        }

        //--------------------------------------------------
        void InstantItem(Transform instPoint_Tf)
        {
            int randItemIndex = Random.Range(0, itemData.data.Count);

            ItemValue itemValue_tp = itemData.data[randItemIndex];
            if (itemValue_tp.cont != ItemContent.Weapon)
            {
                Item_M item_Cp_tp = Instantiate(itemValue_tp.item_Pf, instPoint_Tf)
                    .GetComponent<Item_M>();
                itemData.allItem_Cps.Add(item_Cp_tp);
                item_Cp_tp.onTakeAction = OnTakeItemAction;
                item_Cp_tp.itemValue = itemData.data[randItemIndex];
            }
            else
            {
                CharacterWeapon_M weapon_Cp_tp = Instantiate(itemValue_tp.item_Pf, instPoint_Tf)
                    .GetComponent<CharacterWeapon_M>();
                itemData.allWeapon_Cps.Add(weapon_Cp_tp);
                weapon_Cp_tp.validDur = itemValue_tp.effDur;
                weapon_Cp_tp.onCollideWithChaAction = OnTakeChaWeaponAction;
            }
        }

        //--------------------------------------------------
        void InstantGem(Transform instPoint_Tf)
        {
            int randGemIndex = Random.Range(0, gemData.gem_Pfs.Count);

            GameObject gem_GO_tp = Instantiate(gemData.gem_Pfs[randGemIndex], instPoint_Tf);
            Gem_M gem_Cp_tp = gem_GO_tp.GetComponent<Gem_M>();
            gemData.allGem_Cps.Add(gem_Cp_tp);
            gemData.inBoxGem_Cps.Add(gem_Cp_tp);

            gem_Cp_tp.OnUnhide();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events from external
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events from external

        //--------------------------------------------------
        public void OnBreakBoxAction(GameObject other_GO_pr, Box_M box_Cp_pr)
        {
            if (!other_GO_pr.CompareTag("CharacterLeg"))
            {
                return;
            }

            GameObject root_GO_tp = other_GO_pr.transform.root.gameObject;
            PlayerType pType = PlayerManager.GetPlayerType(root_GO_tp);
            if (pType != PlayerType.Cha)
            {
                return;
            }

            Character_M chat_Cp_tp = root_GO_tp.GetComponent<Character_M>();
            if (!chat_Cp_tp.IsValidBreakBox())
            {
                return;
            }

            //
            int boxIndex_tp = 0;
            foreach (int key_tp in boxData.box_GOs.Keys)
            {
                if (boxData.box_GOs[key_tp] == box_Cp_pr.gameObject)
                {
                    boxIndex_tp = key_tp;
                    break;
                }
            }

            // instant item or gem
            bool genGemSucc_tp = Random.value <= boxData.gemGenChance ? true : false;

            if (genGemSucc_tp)
            {
                InstantGem(boxData.spawnPoint_Tfs[boxIndex_tp]);
            }
            else
            {
                InstantItem(boxData.spawnPoint_Tfs[boxIndex_tp]);
            }

            //
            box_Cp_pr.BreakBox();
        }

        //--------------------------------------------------
        public void OnTakeItemAction(GameObject other_GO_pr, Item_M item_Cp_pr)
        {
            if (!other_GO_pr.CompareTag("Character"))
            {
                return;
            }

            //
            Character_M cha_Cp_tp = other_GO_pr.transform.root.GetComponent<Character_M>();
            cha_Cp_tp.TakeItem(item_Cp_pr);
        }

        //--------------------------------------------------
        public void OnTakeChaWeaponAction(Character_M cha_Cp_pr, CharacterWeapon_M chaWeapon_Cp_pr)
        {
            cha_Cp_pr.EquipWeapon(chaWeapon_Cp_pr);
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

}