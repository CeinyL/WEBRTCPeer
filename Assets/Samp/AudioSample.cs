using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.WebRTC
{
    class AudioSample : MonoBehaviour
    {
        [SerializeField] private AudioSource inputAudioSource;
        [SerializeField] private AudioSource outputAudioSource;
        [SerializeField] private Toggle toggleEnableMicrophone;
        [SerializeField] private Button buttonStart;
        [SerializeField] private Button buttonCall;
        [SerializeField] private Button buttonPause;
        [SerializeField] private Button buttonResume;
        [SerializeField] private Button buttonHangup;
        [SerializeField] private Text textBandwidth;

        private RTCPeerConnection _pc1, _pc2;
        private MediaStream _sendStream;
        private MediaStream _receiveStream;
        private AudioClip m_clipInput;
        private AudioStreamTrack m_audioTrack;

        int m_samplingFrequency = 48000;
        int m_lengthSeconds = 1;

        private string m_deviceName = null;
        void Start()
        {
            StartCoroutine(LoopStatsCoroutine());
            toggleEnableMicrophone.isOn = true;
            buttonStart.onClick.AddListener(OnStart);
            buttonCall.onClick.AddListener(OnCall);
            buttonPause.onClick.AddListener(OnPause);
            buttonResume.onClick.AddListener(OnResume);
            buttonHangup.onClick.AddListener(OnHangUp);
        }
        void OnStart()
        {
            if (toggleEnableMicrophone.isOn)
            {
                m_clipInput = Microphone.Start(m_deviceName, true, m_lengthSeconds, m_samplingFrequency);
                while (!(Microphone.GetPosition(m_deviceName) > 0)) {}
            }
            inputAudioSource.loop = true;
            inputAudioSource.clip = m_clipInput;
            inputAudioSource.Play();

            buttonStart.interactable = false;
            buttonCall.interactable = true;
            buttonHangup.interactable = true;
        }

        private void OnGUI()
        {
            
        }
        void OnCall()
        {
            buttonCall.interactable = false;
            buttonPause.interactable = true;
            //dropdownBandwidth.interactable = true;

            _receiveStream = new MediaStream();
            _receiveStream.OnAddTrack += OnAddTrack;
            _sendStream = new MediaStream();

            var configuration = GetSelectedSdpSemantics();
            _pc1 = new RTCPeerConnection(ref configuration)
            {
                OnNegotiationNeeded = () => StartCoroutine(PeerNegotiationNeeded())
            };

            _pc2 = new RTCPeerConnection(ref configuration)
            {
                OnTrack = e => _receiveStream.AddTrack(e.Track),
            };
            _pc1.OnIceCandidate = candidate => 
            {
                var op = _pc2.AddIceCandidate(candidate);
                if(op)
                {
                    Debug.Log("<color=pink> Added candidate to pc1: </color>" + candidate.Candidate);
                }
            };
            _pc2.OnIceCandidate = candidate => 
            {
                var op = _pc1.AddIceCandidate(candidate);
                if(op)
                {
                    Debug.Log("<color=pink> Added candidate to pc2: </color>" + candidate.Candidate);
                }
            };

        _pc1.OnConnectionStateChange = e => Debug.Log("<color=red> OnConnectionStateChange</color> <color=magenta> 1</color> : " + e);
        _pc1.OnIceConnectionChange = e => Debug.Log("<color=green>OnIceConnectionChange</color> <color=magenta> 1</color> : " + e);
        _pc1.OnIceGatheringStateChange= e => Debug.Log("<color=cyan>OnIceGatheringStateChange</color><color=magenta> 1</color> : " + e); _pc1.OnDataChannel = channel => Debug.Log("data channel create 1");
       
        _pc2.OnConnectionStateChange = e => Debug.Log("<color=red> OnConnectionStateChange</color> <color=yellow> 2</color> : " + e);
        _pc2.OnIceConnectionChange = e => Debug.Log("<color=green>OnIceConnectionChange</color> <color=yellow> 2</color> : " + e);
        _pc2.OnIceGatheringStateChange= e => Debug.Log("<color=cyan>OnIceGatheringStateChange</color><color=yellow> 2</color> : " + e);

       


            var transceiver2 = _pc2.AddTransceiver(TrackKind.Audio);
            transceiver2.Direction = RTCRtpTransceiverDirection.RecvOnly;

            m_audioTrack = new AudioStreamTrack(inputAudioSource);
            _pc1.AddTrack(m_audioTrack, _sendStream);

            var transceiver1 = _pc1.GetTransceivers().First();
        }

        void OnPause()
        {
            var transceiver1 = _pc1.GetTransceivers().First();
            var track = transceiver1.Sender.Track;
            track.Enabled = false;

            buttonResume.gameObject.SetActive(true);
            buttonPause.gameObject.SetActive(false);
        }

        void OnResume()
        {
            var transceiver1 = _pc1.GetTransceivers().First();
            var track = transceiver1.Sender.Track;
            track.Enabled = true;

            buttonResume.gameObject.SetActive(false);
            buttonPause.gameObject.SetActive(true);
        }

        void OnAddTrack(MediaStreamTrackEvent e)
        {
            var track = e.Track as AudioStreamTrack;
            outputAudioSource.SetTrack(track);
            outputAudioSource.loop = true;
            outputAudioSource.Play();
        }

        void OnHangUp()
        {
            Microphone.End(m_deviceName);
            m_clipInput = null;

            m_audioTrack?.Dispose();
            _receiveStream?.Dispose();
            _sendStream?.Dispose();
            _pc1?.Dispose();
            _pc2?.Dispose();
            _pc1 = null;
            _pc2 = null;

            inputAudioSource.Stop();
            outputAudioSource.Stop();

            buttonStart.interactable = true;
            buttonCall.interactable = false;
            buttonHangup.interactable = false;
            buttonPause.interactable = false;

            buttonResume.gameObject.SetActive(false);
            buttonPause.gameObject.SetActive(true);

            /*dropdownSpeakerMode.interactable = true;
            dropdownDSPBufferSize.interactable = true;
            dropdownAudioCodecs.interactable = true;

            dropdownBandwidth.interactable = false;*/

        }
        private static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

            return config;
        }
        IEnumerator PeerNegotiationNeeded()
        {
            var op = _pc1.CreateOffer();
            
            yield return op;
            
            if (!op.IsError)
            {
                if (_pc1.SignalingState != RTCSignalingState.Stable)
                {
                    yield break;
                }

                yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
            }
            else
            {
                Debug.Log("error");
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }
        private IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
        {
            var op = _pc1.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(_pc1);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }

            var op2 = _pc2.SetRemoteDescription(ref desc);
            yield return op2;
            if (op2.IsError)
            {
                
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
            else
            {
                OnSetRemoteSuccess(_pc2);
            }
            
            var op3 = _pc2.CreateAnswer();
            yield return op3;
            if (!op3.IsError)
            {
                yield return OnCreateAnswerSuccess(op3.Desc);
            }
        }


        IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc)
        {
            var op = _pc2.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(_pc2);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }
            var op2 = _pc1.SetRemoteDescription(ref desc);
            yield return op2;
            if (op2.IsError)
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
            else
            {
                OnSetRemoteSuccess(_pc1);
            }
        }
        private void OnSetLocalSuccess(RTCPeerConnection pc)
        {
            Debug.Log("SetLocalDescription complete");
        }
        private void OnSetRemoteSuccess(RTCPeerConnection pc)
        {
            Debug.Log("SetRemoteDescription complete");
        }
        static void OnSetSessionDescriptionError(ref RTCError error)
        {
            Debug.LogError($"Error Detail Type: {error.message}");
        }
        #region UpdateStats
        private IEnumerator LoopStatsCoroutine()
        {
            while (true)
            {
                yield return StartCoroutine(UpdateStatsCoroutine());
                yield return new WaitForSeconds(5f);
            }
        }
        private IEnumerator UpdateStatsCoroutine()
        {
            RTCRtpSender sender = _pc1?.GetSenders().First();
            if (sender == null)
                yield break;
            RTCStatsReportAsyncOperation op = sender.GetStats();
            yield return op;
            if (op.IsError)
            {
                Debug.LogErrorFormat("RTCRtpSender.GetStats() is failed {0}", op.Error.errorType);
            }
            else
            {
                //UpdateStatsPacketSize(op.Value);
            }
        }
        private RTCStatsReport lastResult = null;
        private void UpdateStatsPacketSize(RTCStatsReport res)
        {
            foreach (RTCStats stats in res.Stats.Values)
            {
                if (!(stats is RTCOutboundRTPStreamStats report))
                {
                    continue;
                }

                long now = report.Timestamp;
                ulong bytes = report.bytesSent;

                if (lastResult != null)
                {
                    if (!lastResult.TryGetValue(report.Id, out RTCStats last))
                        continue;

                    var lastStats = last as RTCOutboundRTPStreamStats;
                    var duration = (double)(now - lastStats.Timestamp) / 1000000;
                    ulong bitrate = (ulong)(8 * (bytes - lastStats.bytesSent) / duration);
                    textBandwidth.text = (bitrate / 1000.0f).ToString("f2");
                    //if (autoScroll.isOn)
                    //{
                    //    statsField.MoveTextEnd(false);
                    //}
                }

            }
            lastResult = res;
        }
        #endregion
    }
}
