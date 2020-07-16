﻿using UnityEngine;
using TouhouCardEngine;
using BJSYGameCore;
using UI;
using System.Threading.Tasks;
using TouhouHeartstone;
using System.Net.Sockets;
using System.Net;
using System;
using System.Linq;
using System.Collections.Generic;
using BJSYGameCore.UI;
using System.Text.RegularExpressions;
namespace Game
{
    public class NetworkingManager : Manager
    {
        [SerializeField]
        private HostManager _host;
        public HostManager host
        {
            get
            {
                if (_host == null)
                    _host = this.findInstance<HostManager>();
                return _host;
            }
        }
        public string address
        {
            get { return host.address; }
        }
        public string[] addresses => host.addresses;

        [SerializeField]
        private ClientManager _client;
        public ClientManager client
        {
            get
            {
                if (_client == null)
                    _client = this.findInstance<ClientManager>();
                return _client;
            }
        }
        private GameManager _gameManager;
        public GameManager gameManager
        {
            get
            {
                if (_gameManager == null) _gameManager = getManager<GameManager>();
                return _gameManager;
            }
        }
        public NetworkingPage ui
        {
            get { return getManager<UIManager>().getObject<NetworkingPage>(); }
        }
        [SerializeField]
        List<RoomInfo> _LANRoomList;
        [SerializeField]
        BJSYGameCore.Timer _LANUpdateTimer = new BJSYGameCore.Timer() { duration = 1 };
        protected override void onAwake()
        {
            base.onAwake();
            host.logger = new ULogger("Host");
            client.logger = new ULogger("Client");

            client.addInvokeTarget(this);
            client.onRoomFound += Client_onRoomFound;
            client.onJoinRoom += Client_onJoinRoom;
            client.onRoomInfoUpdate += Client_onRoomInfoUpdate;
            _LANUpdateTimer.start();
            client.onQuitRoom += Client_onQuitRoom;
            gameManager.onGameEnd += onGameEnd;

            ui.LinkButton.onClick.set(() =>
            {
                displayIPPanel();
            });
            ui.LANButton.onClick.set(() =>
            {
                displayLANPanel();
            });
            ui.RoomButton.interactable = false;
            ui.RoomButton.image.color = Color.gray;
            ui.RoomButton.onClick.set(() =>
            {
                displayRoomPanel();
            });
            ui.ReturnButton.onClick.AddListener(() =>
            {
                ui.parent.display(ui.parent.MainMenu);
            });
        }
        protected void Start()
        {
            client.start();
        }
        protected void Update()
        {
            if (_LANUpdateTimer.isExpired())
            {
                updateAllRooms();
                _LANUpdateTimer.start();
            }
        }
        private void Client_onRoomFound(RoomInfo obj)
        {
            if (obj is RoomInfo tHHRoom)
            {
                int index = _LANRoomList.FindIndex(r => r.id == tHHRoom.id);

                if (index < 0)
                {
                    if (!obj.isSameRoom(host.room))
                    {
                        _LANRoomList.Add(tHHRoom);
                        RoomListItem item = ui.NetworkingPageGroup.LANPanel.RoomScrollView.RoomList.addItem();
                        refreshRoomListItem(item, tHHRoom);
                    }
                }
                else
                {
                    if (!obj.isSameRoom(host.room))
                    {
                        _LANRoomList[index] = tHHRoom;
                        RoomListItem item = ui.NetworkingPageGroup.LANPanel.RoomScrollView.RoomList.getItems()[index];
                        refreshRoomListItem(item, tHHRoom);
                    }
                    else
                    {
                        _LANRoomList.RemoveAt(index);
                        RoomList list = ui.NetworkingPageGroup.LANPanel.RoomScrollView.RoomList;
                        list.removeItem(list.getItems()[index]);
                    }
                }
            }
        }
        async Task updateRoomInfo(RoomInfo roomInfo)
        {
            RoomInfo newRoomInfo = await client.checkRoomInfo(roomInfo);
            if (newRoomInfo != null)
            {
                int index = _LANRoomList.FindIndex(r => r.isSameRoom(roomInfo));
                if (index >= -1)
                {
                    _LANRoomList[index] = newRoomInfo;
                    RoomList list = ui.NetworkingPageGroup.LANPanel.RoomScrollView.RoomList;
                    RoomListItem item = list.getItems()[index];
                    refreshRoomListItem(item, newRoomInfo);
                }
            }
            else
            {
                int index = _LANRoomList.FindIndex(r => r.isSameRoom(roomInfo));
                if (index > -1)
                {
                    _LANRoomList.RemoveAt(index);
                    RoomList list = ui.NetworkingPageGroup.LANPanel.RoomScrollView.RoomList;
                    list.removeItem(list.getItems()[index]);
                }
            }
        }
        private void Client_onJoinRoom(RoomInfo obj)
        {
            displayRoomPanel();
            setRoomPanel(obj);
        }
        private void Client_onRoomInfoUpdate(RoomInfo now, RoomInfo updated)
        {
            if (updated.playerList.Count < now.playerList.Count)
            {
                //有玩家离开了
                var roomPlayer = now.playerList.First(p => !updated.playerList.Exists(p2 => p2.id == p.id));
                if (getManager<GameManager>().game is THHGame game)
                {
                    game.getPlayer(roomPlayer.id).cmdSurrender(game);
                }
            }
            setRoomPanel(updated);
        }
        private void Client_onQuitRoom()
        {
            displayLANPanel();
        }
        private void onGameEnd()
        {
            client.quitRoom();
        }
        #region Logic
        /// <summary>
        /// 创建一个房间
        /// </summary>
        /// <returns></returns>
        public async Task createRoom()
        {
            host.start(_port);
            RoomInfo room = host.openRoom(new RoomInfo());
            room.setOption(new GameOption()
            {
                randomSeed = (int)DateTime.Now.ToBinary()
            });
            displayLoadingPanel();
            RoomPlayerInfo playerInfo = new RoomPlayerInfo();
            playerInfo.setProp(RoomPlayerInfoName.DECK_INTARRAY, getManager<GameManager>().deck);
            await client.joinRoom(room, playerInfo);
            ui.RoomButton.interactable = true;
            ui.RoomButton.image.color = Color.white;
        }
        public void findLANRooms()
        {
            client.findRoom(_port);
        }
        public void flushLANRooms()
        {
            _LANRoomList.Clear();
            ui.NetworkingPageGroup.LANPanel.RoomScrollView.RoomList.clearItems();
            findLANRooms();
        }
        public void updateAllRooms()
        {
            foreach (var room in _LANRoomList)
            {
                _ = updateRoomInfo(room);
            }
        }
        public async Task joinRoom(RoomInfo roomInfo)
        {
            if (roomInfo == null)
                throw new ArgumentNullException(nameof(roomInfo));
            displayLoadingPanel();
            RoomPlayerInfo playerInfo = new RoomPlayerInfo();
            playerInfo.setProp(RoomPlayerInfoName.DECK_INTARRAY, getManager<GameManager>().deck);
            await client.joinRoom(roomInfo, playerInfo);
            ui.RoomButton.interactable = true;
            ui.RoomButton.image.color = Color.white;
        }
        public void start()
        {
            getManager<GameManager>().startRemoteGame(client, client.roomInfo.getOption(), client.roomInfo.playerList.ToArray());
        }
        public void quitRoom()
        {
            if (host.RoomIsValid)
                host.closeRoom();
            if (client.roomInfo != null)
                client.quitRoom();
            ui.RoomButton.interactable = false;
            ui.RoomButton.image.color = Color.gray;
            updateAllRooms();
        }
        #endregion
        #region UI
        void displayLoadingPanel()
        {
            ui.NetworkingPageGroup.display(null);
        }
        void displayIPPanel()
        {
            IPPanel ipPanel = ui.NetworkingPageGroup.IPPanel;
            ui.NetworkingPageGroup.display(ipPanel);
            ipPanel.ConnectButton.onClick.set(() =>
            {
                int port = _port;
                string address = "";
                var uri = new Uri("http://" + ipPanel.IPInputField.text);

                if (uri.HostNameType == UriHostNameType.Dns)
                {
                    address = Dns.GetHostAddresses(uri.Host).FirstOrDefault(e => e.AddressFamily == AddressFamily.InterNetwork)?.ToString();
                }
                else
                {
                    address = uri.Host;
                }

                if (uri.Port != 80)
                    port = uri.Port;

                _ = joinRoom(new RoomInfo()
                {
                    ip = address,
                    port = port
                });

                // getManager<UIManager>().getObject<Dialog>().display("输入的地址格式有误", null);
            });
            ipPanel.AddressText.text = "你的地址：\n" + string.Join("\n", addresses);
        }
        [SerializeField]
        int _port;
        public void displayLANPanel()
        {
            LANPanel lanPanel = ui.NetworkingPageGroup.LANPanel;
            ui.NetworkingPageGroup.display(lanPanel);
            lanPanel.FlushRoomButton.onClick.set(() =>
            {
                flushLANRooms();
            });
            lanPanel.CreateRoomButton.interactable = !host.RoomIsValid;
            lanPanel.CreateRoomButton.onClick.set(() =>
            {
                _ = createRoom();
            });
            lanPanel.NameText.text = null;
            lanPanel.IPText.text = null;
            lanPanel.DescText.text = null;
            findLANRooms();
        }
        private void refreshRoomListItem(RoomListItem item, RoomInfo obj)
        {
            item.RoomNameText.text = obj.ip + ":" + obj.port;
            item.asButton.onClick.set(() =>
            {
                LANPanel lanPanel = ui.NetworkingPageGroup.LANPanel;
                if (obj == null)
                {
                    lanPanel.NameText.text = null;
                    lanPanel.IPText.text = null;
                    lanPanel.DescText.text = null;
                }
                else
                {
                    lanPanel.NameText.text = "房间";
                    lanPanel.IPText.text = "IP:" + obj.ip;
                    lanPanel.DescText.text = "描述:";
                }
            });
            item.Button.onClick.set(() =>
            {
                _ = joinRoom(obj);
            });
        }
        private void setRoomPanel(RoomInfo room)
        {
            RoomPanel roomPanel = ui.NetworkingPageGroup.RoomPanel;
            roomPanel.RoomPlayerList.clearItems();
            foreach (var player in room.playerList)
            {
                var item = roomPanel.RoomPlayerList.addItem();
                item.NameText.text = player.name;
                item.CharacterText.text = getManager<CardManager>().getSkin(player.getProp<int[]>(RoomPlayerInfoName.DECK_INTARRAY)[0]).name;
                item.Button.onClick.AddListener(() =>
                {
                    //TODO:查看卡组
                });
            }
            roomPanel.RandomSeedInputField.text = room.getOption().randomSeed.ToString();
            roomPanel.ShuffleToggle.isOn = room.getOption().shuffle;
            roomPanel.InitReplaceTimeInputField.text = room.getOption().timeoutForInitReplace.ToString();
            roomPanel.TurnTimeInputField.text = room.getOption().timeoutForTurn.ToString();
            Debug.Log("刷新房间设定" + roomPanel.IsSortedToggle.isOn);
            roomPanel.IsSortedToggle.isOn = room.getOption().sortedPlayers != null;
            if (host.RoomIsValid)
            {
                roomPanel.RandomSeedInputField.setSelectable(true);
                roomPanel.RandomSeedInputField.onValueChanged.set(value =>
                {
                    if (host.room is RoomInfo roomInfo)
                    {
                        if (int.TryParse(value, out int i))
                        {
                            roomInfo.getOption().randomSeed = i;
                            host.updateRoomInfo(roomInfo);
                        }
                        else
                            roomPanel.RandomSeedInputField.text = roomInfo.getOption().randomSeed.ToString();
                    }
                });
                roomPanel.ShuffleToggle.setSelectable(true);
                roomPanel.ShuffleToggle.onValueChanged.set(value =>
                {
                    if (host.room is RoomInfo roomInfo)
                    {
                        roomInfo.getOption().shuffle = value;
                        host.updateRoomInfo(roomInfo);
                    }
                });
                roomPanel.InitReplaceTimeInputField.setSelectable(true);
                roomPanel.InitReplaceTimeInputField.onEndEdit.set(value =>
                {
                    if (host.room is RoomInfo roomInfo)
                    {
                        if (int.TryParse(value, out int i) && 5 < i)
                        {
                            roomInfo.getOption().timeoutForInitReplace = i;
                            host.updateRoomInfo(roomInfo);
                        }
                        else
                        {
                            roomPanel.InitReplaceTimeInputField.text = roomInfo.getOption().timeoutForInitReplace.ToString();
                        }
                    }
                });
                roomPanel.TurnTimeInputField.setSelectable(true);
                roomPanel.TurnTimeInputField.onEndEdit.set(value =>
                {
                    if (host.room is RoomInfo roomInfo)
                    {
                        if (int.TryParse(value, out int i) && 5 < i)
                        {
                            roomInfo.getOption().timeoutForTurn = i;
                            host.updateRoomInfo(roomInfo);
                        }
                        else
                        {
                            roomPanel.TurnTimeInputField.text = roomInfo.getOption().timeoutForTurn.ToString();
                        }
                    }
                });
                roomPanel.IsSortedToggle.setSelectable(true);
                roomPanel.IsSortedToggle.onValueChanged.set(onValueChanged);
                void onValueChanged(bool value)
                {
                    if (host.room is RoomInfo roomInfo)
                    {
                        Debug.Log("更新房间设定" + value);
                        roomInfo.setOption(new GameOption(roomInfo.getOption())
                        {
                            sortedPlayers = value ? roomInfo.playerList.Select(p => p.id).ToArray() : null
                        });
                        host.updateRoomInfo(roomInfo);
                    }
                }
                roomPanel.StartButton.setSelectable(room.isSameRoom(host.room) && room.playerList.Count > 1);
                roomPanel.StartButton.onClick.set(() =>
                {
                    _ = host.invokeAll<object>(host.room.playerList.Select(p => p.id).ToArray(), nameof(start));
                });
            }
            else
            {
                roomPanel.RandomSeedInputField.setSelectable(false);
                roomPanel.ShuffleToggle.setSelectable(false);
                roomPanel.InitReplaceTimeInputField.setSelectable(false);
                roomPanel.TurnTimeInputField.setSelectable(false);
                roomPanel.IsSortedToggle.setSelectable(false);
                roomPanel.StartButton.setSelectable(false);
            }
            roomPanel.QuitButton.onClick.set(() =>
            {
                quitRoom();
            });
        }



        private void displayRoomPanel()
        {
            ui.NetworkingPageGroup.display(ui.NetworkingPageGroup.RoomPanel);
        }
        #endregion
    }
    public static class RoomInfoName
    {
        public const string OPTION_GAMEOPTION = "option";
        public static GameOption getOption(this RoomInfo roomInfo)
        {
            return roomInfo.getProp<GameOption>(OPTION_GAMEOPTION);
        }
        public static void setOption(this RoomInfo roomInfo, GameOption value)
        {
            roomInfo.setProp(OPTION_GAMEOPTION, value);
        }
    }
    public static class RoomPlayerInfoName
    {
        public const string DECK_INTARRAY = "deck";
        public static int[] getDeck(this RoomPlayerInfo playerInfo)
        {
            return playerInfo.getProp<int[]>(DECK_INTARRAY);
        }
        public static void setDeck(this RoomPlayerInfo playerInfo, int[] value)
        {
            playerInfo.setProp(DECK_INTARRAY, value);
        }
    }
}