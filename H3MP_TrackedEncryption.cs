﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedEncryption : MonoBehaviour
    {
        public TNH_EncryptionTarget physicalEncryptionScript;
        public H3MP_TrackedEncryptionData data;

        // Unknown tracked ID queues
        public static Dictionary<int, int> unknownControlTrackedIDs = new Dictionary<int, int>();
        public static List<int> unknownDestroyTrackedIDs = new List<int>();

        public bool sendDestroy = true; // To prevent feeback loops
        public static int skipDestroy;

        private void OnDestroy()
        {
            H3MP_GameManager.trackedEncryptionByEncryption.Remove(physicalEncryptionScript);

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our Encryptions to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0 && H3MP_GameManager.TNHInstances.TryGetValue(H3MP_GameManager.instance, out H3MP_TNHInstance actualInstance))
                    {
                        int otherPlayer = -1;
                        for(int i=0; i < actualInstance.currentlyPlaying.Count; ++i)
                        {
                            if (actualInstance.currentlyPlaying[i] != H3MP_GameManager.ID)
                            {
                                otherPlayer = actualInstance.currentlyPlaying[i];
                                break;
                            }
                        }

                        if (otherPlayer != -1)
                        {
                            H3MP_ServerSend.GiveEncryptionControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        H3MP_ServerSend.DestroyEncryption(data.trackedID);
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    H3MP_Server.encryptions[data.trackedID] = null;
                    H3MP_Server.availableEncryptionIndices.Add(data.trackedID);
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.encryptions[data.localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
                    H3MP_GameManager.encryptions[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
                    data.localTrackedID = -1;
                }
            }
            else
            {
                bool removeFromLocal = true;
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID && H3MP_GameManager.TNHInstances.TryGetValue(H3MP_GameManager.instance, out H3MP_TNHInstance actualInstance))
                    {
                        int otherPlayer = -1;
                        for (int i = 0; i < actualInstance.currentlyPlaying.Count; ++i)
                        {
                            if (actualInstance.currentlyPlaying[i] != H3MP_GameManager.ID)
                            {
                                otherPlayer = actualInstance.currentlyPlaying[i];
                                break;
                            }
                        }

                        if (otherPlayer != -1)
                        {
                            if (data.trackedID == -1)
                            {
                                if (unknownControlTrackedIDs.ContainsKey(data.localTrackedID))
                                {
                                    unknownControlTrackedIDs[data.localTrackedID] = otherPlayer;
                                }
                                else
                                {
                                    unknownControlTrackedIDs.Add(data.localTrackedID, otherPlayer);
                                }

                                // We want to keep it in local until we give control
                                removeFromLocal = false;
                            }
                            else
                            {
                                H3MP_ClientSend.GiveEncryptionControl(data.trackedID, otherPlayer);

                                // Also change controller locally
                                data.controller = otherPlayer;
                            }
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        if (data.trackedID == -1)
                        {
                            if (!unknownDestroyTrackedIDs.Contains(data.localTrackedID))
                            {
                                unknownDestroyTrackedIDs.Add(data.localTrackedID);
                            }

                            // We want to keep it in local until we give destruction order
                            removeFromLocal = false;
                        }
                        else
                        {
                            H3MP_ClientSend.DestroyEncryption(data.trackedID);

                            H3MP_Client.encryptions[data.trackedID] = null;
                        }
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    if(data.trackedID != -1)
                    {
                        H3MP_Client.encryptions[data.trackedID] = null;
                    }
                }
                if (removeFromLocal && data.localTrackedID != -1)
                {
                    data.RemoveFromLocal();
                }
            }
        }
    }
}
