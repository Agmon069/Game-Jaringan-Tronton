
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

public class ManajerJaringan : MonoBehaviourPunCallbacks
{
    [Header("Connection Status")]
    public TMP_Text statusKoneksi;

    [Header("Login UI")]
    public TMP_InputField NamaPlayer;
    public GameObject PanelLogin;

    [Header("Room UI")]
    public GameObject RoomPanel;
    public GameObject BuatRoomPanel;
    public GameObject RoomAcakPanel;
    public GameObject DaftarRoomPanel;
    public GameObject GamePanel;

    [Header("Create Room UI")]
    public TMP_InputField InputNamaRoom;
    public TMP_Dropdown DropdownMaxPlayer;

    [Header("Room List")]
    public Transform ContentRoomList;
    public GameObject DaftarRoomPrefab;

    [Header("Game Panel")]
    public TMP_Text RoomInfoText;
    public GameObject TombolMulaiGame;

    [Header("Player List")]
    public GameObject daftarplayerPrefab;
    public GameObject daftarplayerContent;

    private Dictionary<string, RoomInfo> daftarRoom =
        new Dictionary<string, RoomInfo>();

    private Dictionary<int, GameObject> daftarPlayer =
        new Dictionary<int, GameObject>();

    private Dictionary<string, GameObject> entriRoom =
        new Dictionary<string, GameObject>();

    private bool isStartingGame = false;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        ActivatePanel(PanelLogin.name);

        if (statusKoneksi != null)
            statusKoneksi.text = "Status Koneksi: Disconnected";
    }

    private void Update()
    {
        if (statusKoneksi != null)
        {
            statusKoneksi.text =
                "Status Koneksi: " +
                PhotonNetwork.NetworkClientState;
        }
    }

    // LOGIN

    public void OnLoginButtonClicked()
    {
        if (NamaPlayer == null ||
            string.IsNullOrWhiteSpace(NamaPlayer.text))
        {
            TampilkanStatus("Masukkan nama player!");
            return;
        }

        PhotonNetwork.NickName = NamaPlayer.text.Trim();
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Berhasil terhubung ke Photon!");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        ActivatePanel(RoomPanel.name);
        Debug.Log("Berhasil masuk lobby.");
    }

    // PANEL

    public void ActivatePanel(string panel)
    {
        if (PanelLogin != null)
            PanelLogin.SetActive(panel == PanelLogin.name);

        if (RoomPanel != null)
            RoomPanel.SetActive(panel == RoomPanel.name);

        if (BuatRoomPanel != null)
            BuatRoomPanel.SetActive(panel == BuatRoomPanel.name);

        if (RoomAcakPanel != null)
            RoomAcakPanel.SetActive(panel == RoomAcakPanel.name);

        if (DaftarRoomPanel != null)
            DaftarRoomPanel.SetActive(panel == DaftarRoomPanel.name);

        if (GamePanel != null)
            GamePanel.SetActive(panel == GamePanel.name);
    }

    public void CancelButton()
    {
        ActivatePanel(RoomPanel.name);
    }

    public void OnCancelButtonClicked()
    {
        CancelButton();
    }

    // CREATE ROOM

    public void CreateRoom()
    {
        string namaRoom = InputNamaRoom != null
            ? InputNamaRoom.text.Trim()
            : "";

        if (string.IsNullOrWhiteSpace(namaRoom))
        {
            namaRoom = "Room " + Random.Range(1000, 10000);
        }

        if (DropdownMaxPlayer == null)
        {
            TampilkanStatus("Dropdown jumlah pemain belum diatur!");
            return;
        }

        int maxPlayer = DropdownMaxPlayer.value + 2;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayer,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(namaRoom, options);
    }

    public void OnRoomCreateButtonClicked()
    {
        CreateRoom();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room berhasil dibuat: " +
                  PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnCreateRoomFailed(
        short returnCode, string message)
    {
        Debug.LogWarning("Gagal membuat room: " + message);
        TampilkanStatus("Gagal membuat room: " + message);
    }

    // JOIN ROOM

    public override void OnJoinedRoom()
    {
        Debug.Log("Bergabung ke room: " +
                  PhotonNetwork.CurrentRoom.Name);

        if (DiepMultiplayer.SoundManager.Instance != null)
        {
            DiepMultiplayer.SoundManager.Instance.PlayJoinRoom();
        }

        ActivatePanel(GamePanel.name);
        isStartingGame = false;

        if (TombolMulaiGame != null)
        {
            TombolMulaiGame.SetActive(
                PhotonNetwork.IsMasterClient);
            Button btn = TombolMulaiGame.GetComponent<Button>();
            if (btn != null) btn.interactable = true;
        }

        BersihkanDaftarPemain();
        UpdateRoomInfo();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            TambahkanPemain(player);
        }
    }

    public void JoinRandomRoom()
    {
        ActivatePanel(RoomAcakPanel.name);
        PhotonNetwork.JoinRandomRoom();
    }

    public void OnJoinRandomRoomButtonClicked()
    {
        JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(
        short returnCode, string message)
    {
        Debug.Log("Tidak ada room yang tersedia: " + message);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 6,
            IsVisible = true,
            IsOpen = true
        };

        string namaRoom = "Room " + Random.Range(1000, 10000);

        PhotonNetwork.CreateRoom(namaRoom, options);
    }

    public override void OnJoinRoomFailed(
        short returnCode, string message)
    {
        Debug.LogWarning("Gagal bergabung ke room: " + message);
        TampilkanStatus("Gagal bergabung ke room.");
    }

    // ROOM INFORMATION

    private void UpdateRoomInfo()
    {
        if (RoomInfoText == null ||
            PhotonNetwork.CurrentRoom == null)
            return;

        RoomInfoText.text =
            "Nama Room: " + PhotonNetwork.CurrentRoom.Name +
            "\nPlayer: " +
            PhotonNetwork.CurrentRoom.PlayerCount + "/" +
            PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    // PLAYER LIST

    private void TambahkanPemain(Player player)
    {
        if (daftarplayerPrefab == null ||
            daftarplayerContent == null)
        {
            Debug.LogWarning("Prefab atau Content pemain belum diatur.");
            return;
        }

        if (daftarPlayer.ContainsKey(player.ActorNumber))
            return;

        GameObject obj = Instantiate(
            daftarplayerPrefab,
            daftarplayerContent.transform);

        obj.transform.localScale = Vector3.one;

        Transform namaTransform =
            obj.transform.Find("namaPlayerText");

        TMP_Text namaText = namaTransform != null
            ? namaTransform.GetComponent<TMP_Text>()
            : obj.GetComponentInChildren<TMP_Text>();

        if (namaText != null)
        {
            namaText.text = player.NickName;
        }
        else
        {
            Debug.LogWarning(
                "TMP_Text nama pemain tidak ditemukan pada prefab.");
        }

        Transform indikator =
            obj.transform.Find("IndikatorPlayer");

        if (indikator != null)
        {
            indikator.gameObject.SetActive(
                player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        }

        daftarPlayer.Add(player.ActorNumber, obj);
    }

    private void BersihkanDaftarPemain()
    {
        foreach (GameObject obj in daftarPlayer.Values)
        {
            if (obj != null)
                Destroy(obj);
        }

        daftarPlayer.Clear();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (DiepMultiplayer.SoundManager.Instance != null)
        {
            DiepMultiplayer.SoundManager.Instance.PlayJoinRoom();
        }

        TambahkanPemain(newPlayer);
        UpdateRoomInfo();

        if (TombolMulaiGame != null)
        {
            TombolMulaiGame.SetActive(
                PhotonNetwork.IsMasterClient);
            Button btn = TombolMulaiGame.GetComponent<Button>();
            if (btn != null) btn.interactable = true;
        }

        Debug.Log(newPlayer.NickName + " masuk ke room.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (daftarPlayer.TryGetValue(
            otherPlayer.ActorNumber, out GameObject obj))
        {
            if (obj != null)
                Destroy(obj);

            daftarPlayer.Remove(otherPlayer.ActorNumber);
        }

        UpdateRoomInfo();

        Debug.Log(otherPlayer.NickName + " keluar dari room.");
    }

    // START GAME

    public void OnStartGameButtonClicked()
    {
        if (isStartingGame)
        {
            Debug.Log("[ManajerJaringan] Game sedang memuat, klik ganda diabaikan.");
            return;
        }

        Debug.Log($"[ManajerJaringan] Tombol Mulai diklik! IsMasterClient={PhotonNetwork.IsMasterClient}, InRoom={PhotonNetwork.InRoom}");

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            isStartingGame = true;
            if (TombolMulaiGame != null)
            {
                Button btn = TombolMulaiGame.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
            Debug.Log("[ManajerJaringan] Memuat level GameScene...");
            PhotonNetwork.LoadLevel("GameScene");
        }
        else
        {
            Debug.LogWarning("[ManajerJaringan] Hanya Master Client yang dapat memulai game!");
        }
    }

    // ROOM LIST

    public void OnShowRoomListButtonClicked()
    {
        ActivatePanel(DaftarRoomPanel.name);

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            UpdateRoomList();
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList ||
                !room.IsVisible ||
                !room.IsOpen)
            {
                daftarRoom.Remove(room.Name);
            }
            else
            {
                daftarRoom[room.Name] = room;
            }
        }

        UpdateRoomList();
    }

    private void UpdateRoomList()
    {
        if (ContentRoomList == null || DaftarRoomPrefab == null)
            return;

        foreach (Transform child in ContentRoomList)
        {
            Destroy(child.gameObject);
        }

        entriRoom.Clear();

        foreach (RoomInfo room in daftarRoom.Values)
        {
            if (!room.IsVisible ||
                !room.IsOpen ||
                room.PlayerCount >= room.MaxPlayers)
            {
                continue;
            }

            GameObject obj = Instantiate(
                DaftarRoomPrefab,
                ContentRoomList);

            obj.transform.localScale = Vector3.one;

            TMP_Text[] texts =
                obj.GetComponentsInChildren<TMP_Text>(true);

            TMP_Text namaText = CariText(obj, "NamaRoomText");
            TMP_Text jumlahText = CariText(obj, "MaksPlayerText");

            // Fallback jika prefab menggunakan dua TMP_Text
            // tanpa nama child yang sesuai.
            if (namaText == null && texts.Length > 0)
                namaText = texts[0];

            if (jumlahText == null && texts.Length > 1)
                jumlahText = texts[1];

            if (namaText != null)
                namaText.text = room.Name;

            if (jumlahText != null)
            {
                jumlahText.text =
                    room.PlayerCount + "/" + room.MaxPlayers;
            }

            Button tombol =
                obj.GetComponentInChildren<Button>(true);

            if (tombol != null)
            {
                string namaRoom = room.Name;

                tombol.onClick.AddListener(() =>
                {
                    if (DiepMultiplayer.SoundManager.Instance != null)
                    {
                        DiepMultiplayer.SoundManager.Instance.PlayButtonClick();
                    }
                    PhotonNetwork.JoinRoom(namaRoom);
                });
            }

            entriRoom[room.Name] = obj;
        }
    }

    private TMP_Text CariText(GameObject obj, string namaChild)
    {
        Transform child = obj.transform.Find(namaChild);

        if (child == null)
            return null;

        return child.GetComponent<TMP_Text>();
    }

    public override void OnLeftLobby()
    {
        daftarRoom.Clear();
        UpdateRoomList();
    }

    // LEAVE ROOM

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    public void OnLeaveGameButtonClicked()
    {
        LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        BersihkanDaftarPemain();
        UpdateRoomInfo();

        if (PhotonNetwork.InLobby)
        {
            ActivatePanel(RoomPanel.name);
        }
        else
        {
            ActivatePanel(PanelLogin.name);

        if (TombolMulaiGame != null)
        {
            Button btn = TombolMulaiGame.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnStartGameButtonClicked);
                btn.onClick.AddListener(OnStartGameButtonClicked);
            }
        }
        }
    }

    // QUIT GAME

    public void QuitGame()
    {
        Application.Quit();
    }

    private void TampilkanStatus(string pesan)
    {
        if (statusKoneksi != null)
            statusKoneksi.text = pesan;

        Debug.Log(pesan);
    }
}
