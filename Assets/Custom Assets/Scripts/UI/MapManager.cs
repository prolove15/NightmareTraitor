using PhantomBetrayal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhantomBetrayal
{
    public class MapManager : MonoBehaviour
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
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- static fields
        public static MapManager instance;

        //-------------------------------------------------- serialize fields
        [SerializeField] GameObject myselfDot_Pf, chaDot_Pf, ghostDot_Pf;

        [SerializeField] Sprite traitorMap;

        [SerializeField] Transform mapGroup_Tf;

        [SerializeField] float minX, minY, maxX, maxY;

        //-------------------------------------------------- public fields
        [ReadOnly]
        public List<GameState_En> gameStates = new List<GameState_En>();

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;

        PlayerManager pManager_Cp;

        Dictionary<int, RectTransform> chaDot_RTs = new Dictionary<int, RectTransform>();

        [SerializeField][ReadOnly] RectTransform traitorDot_RT;

        [SerializeField][ReadOnly] RectTransform ghostDot_RT;

        [SerializeField][ReadOnly] int localPlId;

        [SerializeField][ReadOnly] PlayerType localPlType = new PlayerType();

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
        List<Character_M> cha_Cps { get { return pManager_Cp.cha_Cps; } }

        Ghost_M ghost_Cp { get { return pManager_Cp.ghost_Cp; } }

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
            if (ExistGameStates(GameState_En.Playing))
            {
                Handle_Map();
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
            StartCoroutine(Corou_Init());
        }

        IEnumerator Corou_Init()
        {
            AddMainGameState();

            SetComponents();

            InitVariables();

            InstantMapDots();

            mainGameState = GameState_En.Inited;
            yield return null;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
            pManager_Cp = controller_Cp.pManager_Cp;
        }

        //--------------------------------------------------
        void InitVariables()
        {
            localPlId = pManager_Cp.localPlId;
            localPlType = pManager_Cp.localPlType;
        }

        //--------------------------------------------------
        void InstantMapDots()
        {
            //
            for (int i = 0; i < cha_Cps.Count; i++)
            {
                if (localPlType == PlayerType.Cha && localPlId == cha_Cps[i].player_Cp.pData.id)
                {
                    chaDot_RTs.Add(cha_Cps[i].chaData.id,
                        Instantiate(myselfDot_Pf, mapGroup_Tf).GetComponent<RectTransform>());
                }
                else
                {
                    chaDot_RTs.Add(cha_Cps[i].chaData.id,
                        Instantiate(chaDot_Pf, mapGroup_Tf).GetComponent<RectTransform>());
                }
            }

            //
            if (localPlType == PlayerType.Ghost)
            {
                ghostDot_RT = Instantiate(myselfDot_Pf, mapGroup_Tf).GetComponent<RectTransform>();
            }
            else
            {
                ghostDot_RT = Instantiate(ghostDot_Pf, mapGroup_Tf).GetComponent<RectTransform>();
            }

            // set sibling
            if (localPlType == PlayerType.Cha)
            {
                ghostDot_RT.SetAsLastSibling();
                chaDot_RTs[localPlId].transform.SetAsLastSibling();
            }
            else if (localPlType == PlayerType.Ghost)
            {
                ghostDot_RT.SetAsLastSibling();
            }

            // set active characters and ghost map
            if (localPlType == PlayerType.Cha)
            {
                SetActiveCharactersMap(true);
                SetActiveGhostMap(false);
            }
            else if (localPlType == PlayerType.Ghost)
            {
                SetActiveCharactersMap(false);
                SetActiveGhostMap(true);
            }

            // destroy prefab in scene
            Destroy(myselfDot_Pf);
            Destroy(chaDot_Pf);
            Destroy(ghostDot_Pf);
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
            AddGameStates(GameState_En.Playing);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle map
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region

        //--------------------------------------------------
        public void Handle_Map()
        {
            foreach (int key_tp in chaDot_RTs.Keys)
            {
                SetMapDot(chaDot_RTs[key_tp], pManager_Cp.GetChaFromId(key_tp).transform);
            }

            SetMapDot(ghostDot_RT, ghost_Cp.transform);
        }

        //--------------------------------------------------
        void SetMapDot(RectTransform dot_RT, Transform tar_Tf_tp)
        {
            Vector2 pos = new Vector2(tar_Tf_tp.position.x, tar_Tf_tp.position.z);

            // Ensure the position is within the specified bounds
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            // Map 3D position to 2D position within the minimap bounds
            float mappedX = Mathf.InverseLerp(minX, maxX, pos.x);
            float mappedY = Mathf.InverseLerp(minY, maxY, pos.y);

            // Set the dot_RT position relative to its parent RectTransform
            dot_RT.anchorMin = new Vector2(mappedX, mappedY);
            dot_RT.anchorMax = new Vector2(mappedX, mappedY);
            dot_RT.rotation = Quaternion.Euler(dot_RT.eulerAngles.x, dot_RT.eulerAngles.y,
                -tar_Tf_tp.rotation.eulerAngles.y);
        }

        //--------------------------------------------------
        void SetActiveCharactersMap(bool flag)
        {
            foreach (RectTransform cha_RT_tp in chaDot_RTs.Values)
            {
                cha_RT_tp.gameObject.SetActive(flag);
            }
        }

        //--------------------------------------------------
        void SetActiveGhostMap(bool flag)
        {
            ghostDot_RT.gameObject.SetActive(flag);
        }

        #endregion

        //--------------------------------------------------
        public void OnGhostDetectedByCha(bool flag)
        {
            SetActiveGhostMap(flag);
        }

        //--------------------------------------------------
        public void OnCharacterDetectedByGho(List<int> detectedChas_pr)
        {
            foreach (int chaId_tp in chaDot_RTs.Keys)
            {
                chaDot_RTs[chaId_tp].gameObject.SetActive(detectedChas_pr.Contains(chaId_tp) ? true : false);
            }
        }

        //--------------------------------------------------
        public void OnCharacterDead(int chaId_pr)
        {
            chaDot_RTs[chaId_pr].gameObject.SetActive(false);
            chaDot_RTs.Remove(chaId_pr);
        }

        //--------------------------------------------------
        public void OnGhostDead()
        {
            SetActiveGhostMap(false);
        }

        //--------------------------------------------------
        public void OnTraitorAppear(int chaId_pr)
        {
            if (localPlType == PlayerType.Ghost)
            {
                chaDot_RTs[chaId_pr].GetComponent<Image>().sprite = traitorMap;

                SetActiveCharactersMap(true);
            }
        }

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