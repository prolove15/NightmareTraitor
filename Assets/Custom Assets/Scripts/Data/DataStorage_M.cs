
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;
using PhantomBetrayal;
using UnityEngine.AI;
using Unity.VisualScripting.FullSerializer;
using System.ComponentModel;

namespace PhantomBetrayal
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Functionalities
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Functionalities

    //--------------------------------------------------
    [Serializable]
    public class HashHandler
    {
        [SerializeField]
        [ReadOnly]
        public static List<Hash128> hashes = new List<Hash128>();

        public static Hash128 RegRandHash()
        {
            Hash128 result = new Hash128();

            do
            {
                result = new Hash128();
                result.Append(Time.time.ToString() + UnityEngine.Random.value.ToString());
            }
            while (hashes.Contains(result));

            hashes.Add(result);

            return result;
        }

        public static void RemoveHash(params Hash128[] hashes_pr)
        {
            foreach (Hash128 hash_tp in hashes_pr)
            {
                if (hashes.Contains(hash_tp))
                {
                    hashes.Remove(hash_tp);
                }
            }
        }

        public static bool ContainsHash(Hash128 hash_pr)
        {
            return hashes.Contains(hash_pr);
        }
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Player
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Player

    //--------------------------------------------------
    public class PlayersData
    {
        public List<Player_M> player_Cps = new List<Player_M>();

        public Player_M this[int id_pr]
        {
            get
            {
                Player_M result = null;

                foreach (Player_M player_Cp_tp in player_Cps)
                {
                    if (player_Cp_tp.pData.id == id_pr)
                    {
                        result = player_Cp_tp;
                        break;
                    }
                }

                return result;
            }
        }
    }

    //--------------------------------------------------
    [Serializable]
    public class PlayerData
    {
        [SerializeField] public int id;

        [SerializeField] public bool isCom;

        [SerializeField] public bool isLocalPlayer;

        [SerializeField] public bool isServer;

        [SerializeField] public PlayerType type;

        [ReadOnly] public bool isTraitor;
    }

    //--------------------------------------------------
    public enum PlayerType
    {
        Null, Cha, Ghost,
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// PrepareData
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region PrepareData

    //--------------------------------------------------
    [Serializable]
    public class PrepareData
    {
        public List<PrepareValue> values = new List<PrepareValue>();
    }

    [Serializable]
    public class PrepareValue
    {
        public int uId;
        public int pId;
        public bool isCom;
        public bool isCha;
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Character
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Character

    //--------------------------------------------------
    [Serializable]
    public class CharactersData
    {
        [SerializeField] public List<GameObject> cha_Pfs = new List<GameObject>();

        [ReadOnly] public List<CharacterData> data = new List<CharacterData>(); 

        [SerializeField] public int chsMaxCount = 5;

        [SerializeField] public float minMoveSpdCoef = 1f, maxMoveSpdCoef = 1.5f;

        [ReadOnly] public float moveSpdCoef;

        [SerializeField] public float minRotSpdCoef = 1f, maxRotSpdCoef = 1.5f;

        [ReadOnly] public float rotSpdCoef;

        [SerializeField] public float spdCoefChangeDur = 0.3f;

        [SerializeField] public Transform spawnPointsGroup_Tf;

        [ReadOnly] public List<Transform> spawnPoint_Tfs = new List<Transform>();

        [SerializeField] public float stoppingDist = 0.05f;

        public void Init()
        {
            moveSpdCoef = minMoveSpdCoef;
            rotSpdCoef = minRotSpdCoef;

            for (int i = 0; i < spawnPointsGroup_Tf.childCount; i++)
            {
                spawnPoint_Tfs.Add(spawnPointsGroup_Tf.GetChild(i));
            }
        }

        public bool ContainsId(int id)
        {
            bool result = false;

            foreach (CharacterData chaData in data)
            {
                if (chaData.id == id)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public CharacterData GetChaData(int id)
        {
            CharacterData chaData = new CharacterData();

            foreach (CharacterData value_tp in data)
            {
                if (value_tp.id == id)
                {
                    chaData = value_tp;
                    break;
                }
            }

            return chaData;
        }
    }

    //--------------------------------------------------
    [Serializable]
    public class CharacterData
    {
        [SerializeField] public int id;

        [SerializeField] public Sprite icon;

        [SerializeField] public Sprite avatar;

        [SerializeField] public string name;

        [SerializeField] public int norHp;

        [ReadOnly] public int hp;

        [SerializeField] public float norAtkInterval = 2f;

        [ReadOnly] public float atkInterval;

        [SerializeField] public int norDmg;

        [ReadOnly] public int dmg;

        [SerializeField] public float norMoveSpd = 0.9f;

        [ReadOnly] public float moveSpd;

        [SerializeField] public float rotSpd = 90f;

        [SerializeField] public float rollDur = 1.2f;

        [SerializeField] public float rollInterval = 3f;

        [ReadOnly] public bool rollCdReseted = true;

        [ReadOnly] public bool alive;

        [SerializeField] public int maxGold;

        [ReadOnly] public int gold;

        public void Init()
        {
            alive = true;
            hp = norHp;
            atkInterval = norAtkInterval;
            dmg = norDmg;
            moveSpd = norMoveSpd;
        }
    }

    //--------------------------------------------------
    [Serializable]
    public class CharacterValue
    {
        [SerializeField] public NavMeshAgent navAgent_Cp;

        [SerializeField] public Animator chatAnim_Cp;

        [SerializeField] public Transform effTriPoint_bottom_Tf, effTriPoint_middle_Tf;

        [SerializeField] public Transform hand_Tf;

        [SerializeField] public float atkActionDur;

        [SerializeField] public float hitValidStartTime, hitValidEndTime;

        [SerializeField] public float takeAtkActionDur;

        [SerializeField] public float breakStartTime, breakEndTime;

        [SerializeField] public bool atkEnable;
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Ghost
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Ghost

    //--------------------------------------------------
    [Serializable]
    public class GhostData
    {
        [SerializeField] public int id;

        [SerializeField] public GameObject ghost_Pf;

        [SerializeField] public string name;

        [SerializeField] public int norHp;

        [ReadOnly] public int hp;

        [SerializeField] public float norAtkInterval;

        [ReadOnly] public float atkInterval;

        [SerializeField] public int norDmg;

        [ReadOnly] public int dmg;

        [SerializeField] public float norMoveSpd;

        [ReadOnly] public float moveSpd;

        [SerializeField] public float rotSpd;

        public void Init()
        {
            hp = norHp;
            atkInterval = norAtkInterval;
            dmg = norDmg;
            moveSpd = norMoveSpd;
        }
    }

    //--------------------------------------------------
    [Serializable]
    public class GhostValue
    {
        [SerializeField] public NavMeshAgent navAgent_Cp;

        [SerializeField] public Animator ghostAnim_Cp;

        [SerializeField] public Transform parEffPoint_Tf;

        [SerializeField] public float atkActionDur;

        [SerializeField] public float takeAtkActionDur;

        [SerializeField] public float breakStartTime, breakEndTime;
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Door
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Door

    //--------------------------------------------------
    public class DoorData
    {
        public List<Door_M> data = new List<Door_M>();

        public Door_M this[Hash128 hash_pr]
        {
            get
            {
                Door_M result = new Door_M();

                foreach (Door_M data_tp in data)
                {
                    if (data_tp.doorValue.id == hash_pr)
                    {
                        result = data_tp;
                        break;
                    }
                }

                return result;
            }
        }
    }

    //--------------------------------------------------
    [Serializable]
    public class DoorValue
    {
        [ReadOnly]
        public Hash128 id;

        [SerializeField]
        public Animator anim_Cp;

        [SerializeField]
        public float animDur;

        [ReadOnly]
        public DoorOpenStatus openStat;
    }

    //--------------------------------------------------
    public enum DoorOpenStatus
    {
        Null = 0, Open = 1, Close = 2, Opening = 3, Closing = 4, Toggle = 5,
    }

    #endregion

    //////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Env
    /// </summary>
    //////////////////////////////////////////////////////////////////////
    #region Env

    //--------------------------------------------------
    [Serializable]
    public class BoxData
    {
        [SerializeField] public GameObject box_Pf;

        [SerializeField] public Transform spawnGroup_Tf;

        [ReadOnly] public List<Transform> spawnPoint_Tfs = new List<Transform>();

        [SerializeField] public float spawnInterMin = 5f, spawnInterMax = 20f;

        [SerializeField] public float disapDur = 5f;

        [SerializeField] public float gemGenChance = 0.1f;

        [ReadOnly] public Dictionary<int, GameObject> box_GOs = new Dictionary<int, GameObject>();
    }

    //--------------------------------------------------
    [Serializable]
    public class ItemData
    {
        [SerializeField] public List<ItemValue> data = new List<ItemValue>();

        [ReadOnly] public List<Item_M> allItem_Cps = new List<Item_M>();

        [ReadOnly] public List<CharacterWeapon_M> allWeapon_Cps = new List<CharacterWeapon_M>();
    }

    [Serializable]
    public class ItemValue
    {
        [SerializeField] public int id;

        [SerializeField] public string title;

        [SerializeField] public string context;

        [SerializeField] public int amount;

        [SerializeField] public GameObject item_Pf;

        [SerializeField] public GameObject eff_Pf;

        [SerializeField] public ItemContent cont;

        [SerializeField] public float effDur;
    }

    public enum ItemContent
    {
        Null, Health, Gold, Weapon,
    }

    //--------------------------------------------------
    [Serializable]
    public class GemData
    {
        [SerializeField] public List<GameObject> gem_Pfs = new List<GameObject>();

        [SerializeField] public int maxGemCount;

        [ReadOnly] public List<Gem_M> allGem_Cps = new List<Gem_M>();

        [ReadOnly] public List<Gem_M> inBoxGem_Cps = new List<Gem_M>();

        [ReadOnly] public List<Gem_M> takenGem_Cps = new List<Gem_M>();

        [ReadOnly] public List<Gem_M> droppedGem_Cps = new List<Gem_M>();
    }

    [Serializable]
    public class GemValue
    {
        [SerializeField] public int id;

        [SerializeField] public string title;
    }

    #endregion
}