using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.Timeline;
using TMPro;
public class WebRTC_Client : MonoBehaviour
{

    private RTCPeerConnection localPeer;
    private RTCPeerConnection remotePeer;

    [SerializeField] private AudioSource _inputAudio;
    [SerializeField] private AudioSource _outputAudio;

    
    [SerializeField] private TMP_InputField offer_textbox;
    [SerializeField] private TMP_InputField answer_textbox;
    private MediaStream localStream;
    private MediaStream remoteStream;

    private string microphone = null;
    //private AudioClip m_clipInput;
    private AudioStreamTrack m_audioTrack;

    int m_samplingFrequency = 48000;
    int m_lengthSeconds = 1;
    
    void Start()
    {


        microphone = Microphone.devices[0];
        var m_clipInput = Microphone.Start(microphone, true, m_lengthSeconds, m_samplingFrequency);
        Debug.Log("Selected microphone: " + microphone);

        _inputAudio.loop = true;
		_inputAudio.clip = m_clipInput;
        _inputAudio.Play();

		m_audioTrack = new AudioStreamTrack(_inputAudio);

        Debug.Log("starting");
        CreatePeers();
    }

    void OnAddTrack(MediaStreamTrackEvent e)
    {
        Debug.Log("OnAddTrack");
        var track = e.Track as AudioStreamTrack;
        _outputAudio.SetTrack(track);
        _outputAudio.loop = true;
        _outputAudio.Play();
    }

    private void CreatePeers()
	{
        Debug.Log("creating peers");
        localPeer = new RTCPeerConnection();
        remotePeer = new RTCPeerConnection();

        localStream = new MediaStream();
        remoteStream = new MediaStream();

        m_audioTrack.Loopback = true;
        remoteStream.OnAddTrack += OnAddTrack;

        localPeer.OnIceCandidate = e =>
        {
            remotePeer.AddIceCandidate(e);
        };
        localPeer.OnNegotiationNeeded = () => StartCoroutine(ExchangeOffer());
        localPeer.OnIceConnectionChange = (e) =>
        {
            Debug.Log($"Local: IceConnectionChange: {e}");
        };
        localPeer.OnConnectionStateChange = (e) =>
        {
            Debug.Log($"Local: ConnectionStateChange: {e}");
        };
        localPeer.OnIceGatheringStateChange = (e) =>
        {
            Debug.Log($"Local: IceGatheringStateChange: {e}");
        };

        remotePeer.OnIceCandidate = e =>
        {
            localPeer.AddIceCandidate(e);
        };
        remotePeer.OnIceConnectionChange = (e) =>
        {
            Debug.Log($"Remote: IceConnectionChange: {e}");
        };
        remotePeer.OnConnectionStateChange = (e) =>
        {
            Debug.Log($"Remote: ConnectionStateChange: {e}");
        };
        remotePeer.OnIceGatheringStateChange = (e) =>
        {
            Debug.Log($"Remote: IceGatheringStateChange: {e}");
        };

        var transceiver2 = remotePeer.AddTransceiver(TrackKind.Audio);
        transceiver2.Direction = RTCRtpTransceiverDirection.RecvOnly;

        remotePeer.OnTrack = (RTCTrackEvent e) => handleRemoteTrack(e);

        
        localPeer.AddTrack(m_audioTrack, localStream);

        foreach (var sender in localPeer.GetSenders())
        {
            Debug.Log("senders: " + sender);
        }

        foreach (var receivers in localPeer.GetReceivers())
        {
            Debug.Log("receivers: " + receivers);
        }

        //yield return StartCoroutine(ExchangeOffer());
	}

    public void handleRemoteTrack(RTCTrackEvent e)
    {
        Debug.Log("HandleRemoteTrack");
        if (e.Track is AudioStreamTrack)
        {
            remoteStream.AddTrack(e.Track);
        }
    }
    bool Offerfill=false;
    bool Answerfill=false;
    [SerializeField]
    [Tooltip("reciver tru to ten co wkleja oferte i kopiuje answer")]
    bool reciver=true;
    [SerializeField]
    [Tooltip("reciver tru to ten co kopiuje oferte i wkleja answer")]
    bool sender=false;
    RTCSessionDescription desc;
    private IEnumerator ExchangeOffer()
	{
        Offerfill=false;
        Answerfill=false;
        offer_textbox.text="";
        answer_textbox.text="";

        if(sender)
        {

        
            var op1 = localPeer.CreateOffer();
            yield return op1;


            desc = op1.Desc;
            var op2 = localPeer.SetLocalDescription(ref desc);
            offer_textbox.text = localPeer.LocalDescription.sdp;
            yield return op2;
        }
        if(Offerfill&&reciver)
        {        
            var offer = offer_textbox.text;
            desc.sdp = offer;

            var op3 = remotePeer.SetRemoteDescription(ref desc); 
            yield return op3;

        
            var op4 = remotePeer.CreateAnswer();
            yield return op4;


            desc = op4.Desc;
            var op5 = remotePeer.SetLocalDescription(ref desc);
            answer_textbox.text = remotePeer.LocalDescription.sdp;
            yield return op5;
        }
        if(Answerfill&sender)
        {
            var answer = answer_textbox.text;
            desc.sdp = answer;
            var op6 = localPeer.SetRemoteDescription(ref desc);
            yield return op6;
        }
        
    }
    void Update()
    {
        if(offer_textbox.text=="")
        {
            Offerfill=true;
        }
        if(answer_textbox.text=="")
        {
            Answerfill=true;
        }
    }
    
}
