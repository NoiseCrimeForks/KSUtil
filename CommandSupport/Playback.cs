//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KSUtil
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Kinect.Tools;

    /// <summary>
    /// Playback class, which supports playing an event file to the Kinect service
    /// </summary>
    public static class Playback
    {
        /// <summary>
        /// Plays an event file to the Kinect service
        /// </summary>
        /// <param name="client">KStudioClient which is connected to the Kinect service</param>
        /// <param name="filePath">Path to event file which is targeted for playback</param>
        /// <param name="streamNames">Collection of streams to include in the playback session</param>
        /// <param name="loopCount">Number of times the playback should be repeated before stopping</param>
        public static void PlaybackClip(KStudioClient client, string filePath, IEnumerable<string> streamNames, uint loopCount)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (!client.IsServiceConnected)
            {
                throw new InvalidOperationException(Strings.ErrorNotConnected);
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            KStudioPlayback playback = null;

            // determine if all specified streams are valid for playback
            if (streamNames.Count<string>() > 0)
            {
                HashSet<Guid> playbackDataTypeIds = StreamSupport.ConvertStreamsToPlaybackGuids(streamNames);
                StreamSupport.VerifyStreamsForRecordAndPlayback(playbackDataTypeIds);
                Playback.VerifyStreamsForPlayback(client, filePath, playbackDataTypeIds);

                try
                {
                    KStudioEventStreamSelectorCollection streams = StreamSupport.CreateStreamCollection(playbackDataTypeIds, false);
                    playback = client.CreatePlayback(filePath, streams);
                }
                catch (Exception)
                {
                    //K4W supports uncompressed and compressed color, so if we get an error, try playing the other type
                    KStudioEventStreamSelectorCollection streams = StreamSupport.CreateStreamCollection(playbackDataTypeIds, true);
                    playback = client.CreatePlayback(filePath, streams);
                }
            }
            else
            {
                playback = client.CreatePlayback(filePath);
            }

            Guid depthGuid = StreamSupport.ConvertStreamStringToGuid( "depth" );

            SimpleSocketUDP udpServer = new SimpleSocketUDP();
            udpServer.Server( "127.0.0.1", 27001 );

            // begin playback
            using ( playback )
            {
                playback.EndBehavior = KStudioPlaybackEndBehavior.Stop; // this is the default behavior
                playback.Mode = KStudioPlaybackMode.TimingDisabled; // .TimingEnabled; // this is the default behavior
                playback.LoopCount = loopCount;

                // We start paused in case we want to step through
                playback.StartPaused();

                while ( playback.State == KStudioPlaybackState.Playing || playback.State == KStudioPlaybackState.Paused || playback.State == KStudioPlaybackState.Busy )
                {
                    // Thread.Sleep( 1 ); // todo - add to configure

                    // if updating frame while another step comes in we get invalid operation!
                    if ( playback.State != KStudioPlaybackState.Busy )
                    {
#if UDP_MSG_TEXT
                        ActOnTextMessage( playback, depthGuid );
#else
                        ActOnByteMessage( playback, depthGuid );
#endif
                    }
                }

                if (playback.State == KStudioPlaybackState.Error)
                {
                    throw new InvalidOperationException(Strings.ErrorPlaybackFailed);
                }
            }

            udpServer.CleanUp();
        }

        /*
        private static void ActOnTextMessage( KStudioPlayback playback, Guid depthGuid )
        {
            // Note - we don't buffer messages so we may miss some!
            if ( !string.IsNullOrEmpty( SimpleSocketUDP.Message ) )
            {
                string lastMessage = SimpleSocketUDP.Message;
                SimpleSocketUDP.Message = null;

                switch ( lastMessage )
                {
                    case "STEP":
                        if ( playback.State != KStudioPlaybackState.Paused )
                        {
                            playback.Mode = KStudioPlaybackMode.TimingDisabled;
                            playback.Pause();
                        }

                        playback.StepOnce( depthGuid );
                        break;

                    case "PLAY":
                        if ( playback.State == KStudioPlaybackState.Playing )
                        {
                            // Swith to Paused Mode - ready for stepping
                            playback.Mode = KStudioPlaybackMode.TimingDisabled;
                            playback.Pause();
                        }
                        else
                        {
                            playback.Mode = KStudioPlaybackMode.TimingEnabled;
                            playback.Resume();
                        }
                        break;

                    case "TIME":
                        // Toggle playback Timing
                        playback.Mode = ( playback.Mode == KStudioPlaybackMode.TimingEnabled ) ? KStudioPlaybackMode.TimingDisabled : KStudioPlaybackMode.TimingEnabled;

                        // Start playing if paused
                        if ( playback.State == KStudioPlaybackState.Paused )
                            playback.Resume();

                        break;

                    case "EXIT":
                        playback.Stop();
                        break;
                }
            }
        }
       
        private static void ActOnByteMessage( KStudioPlayback playback, Guid depthGuid )
        {
            // Note - we don't buffer messages so we may miss some!
            if ( SimpleSocketUDP.MessageByte != 0 )
            {
                byte lastMessage = SimpleSocketUDP.MessageByte;
                SimpleSocketUDP.MessageByte = 0;

                switch ( lastMessage )
                {
                    case (byte)KSUtilCommands.Step:
                        if ( playback.State != KStudioPlaybackState.Paused )
                        {
                            playback.Mode = KStudioPlaybackMode.TimingDisabled;
                            playback.Pause();
                        }

                        playback.StepOnce( depthGuid );
                        break;

                    case (byte)KSUtilCommands.Play:
                        if ( playback.State == KStudioPlaybackState.Playing )
                        {
                            // Swith to Paused Mode - ready for stepping
                            playback.Mode = KStudioPlaybackMode.TimingDisabled;
                            playback.Pause();
                        }
                        else
                        {
                            playback.Mode = KStudioPlaybackMode.TimingEnabled;
                            playback.Resume();
                        }
                        break;

                    case (byte)KSUtilCommands.Time:
                        // Toggle playback Timing
                        playback.Mode = ( playback.Mode == KStudioPlaybackMode.TimingEnabled ) ? KStudioPlaybackMode.TimingDisabled : KStudioPlaybackMode.TimingEnabled;

                        // Start playing if paused
                        if ( playback.State == KStudioPlaybackState.Paused )
                            playback.Resume();

                        break;

                    case (byte)KSUtilCommands.Exit:
                        playback.Stop();
                        break;
                }
            }
        }
        */

        private static void ActOnTextMessage( KStudioPlayback playback, Guid depthGuid )
        {
            // Note - we don't buffer messages so we may miss some!
           
            // Grab Server Message
            string  lastMessage = SimpleSocketUDP.Message;
            // Clear Server Storage
            SimpleSocketUDP.Message = null;

            if ( !string.IsNullOrEmpty( lastMessage ) )
            {
                byte byteID = 0;

                switch ( lastMessage )
                {
                    case "STEP": byteID = ( byte )KSUtilCommands.Step; break;
                    case "PLAY": byteID = ( byte )KSUtilCommands.Play; break;
                    case "TIME": byteID = ( byte )KSUtilCommands.Time; break;
                    case "EXIT": byteID = ( byte )KSUtilCommands.Exit; break;
                    case "CALI": byteID = ( byte )KSUtilCommands.Calibration; break;
                }

                if ( byteID != 0 )
                    ActOnServerMessage( byteID, playback, depthGuid );
            }
        }


        private static void ActOnByteMessage( KStudioPlayback playback, Guid depthGuid )
        {
            // Note - we don't buffer messages so we may miss some!

            // Grab Server Message
            byte lastMessage = SimpleSocketUDP.MessageByte;
            // Clear Server Storage
            SimpleSocketUDP.MessageByte = 0;

            if ( lastMessage != 0 )            
                ActOnServerMessage( lastMessage, playback, depthGuid );            
        }

        private static void ActOnServerMessage( byte lastMessage, KStudioPlayback playback, Guid depthGuid )
        {
            switch ( lastMessage )
            {
                case ( byte )KSUtilCommands.Step:
                    if ( playback.State != KStudioPlaybackState.Paused )
                    {
                        playback.Mode = KStudioPlaybackMode.TimingDisabled;
                        playback.Pause();
                    }

                    playback.StepOnce( depthGuid );
                    break;

                case ( byte )KSUtilCommands.Play:
                    if ( playback.State == KStudioPlaybackState.Playing )
                    {
                        // Swith to Paused Mode - ready for stepping
                        playback.Mode = KStudioPlaybackMode.TimingDisabled;
                        playback.Pause();
                    }
                    else
                    {
                        playback.Mode = KStudioPlaybackMode.TimingEnabled;
                        playback.Resume();
                    }
                    break;

                case ( byte )KSUtilCommands.Time:
                    // Toggle playback Timing
                    playback.Mode = ( playback.Mode == KStudioPlaybackMode.TimingEnabled ) ? KStudioPlaybackMode.TimingDisabled : KStudioPlaybackMode.TimingEnabled;

                    // Start playing if paused
                    if ( playback.State == KStudioPlaybackState.Paused )
                        playback.Resume();

                    break;

                case ( byte )KSUtilCommands.Exit:
                    playback.Stop();
                    break;
            }
        }        

        /// <summary>
        /// Verifies that the streams selected for playback exist in the file and are capable of being played on the service
        /// </summary>
        /// <param name="client">KStudioClient which is connected to the Kinect service</param>
        /// <param name="filePath">Path to file that will be played back</param>
        /// <param name="playbackStreams">Collection of streams which have been selected for playback</param>
        private static void VerifyStreamsForPlayback(KStudioClient client, string filePath, IEnumerable<Guid> playbackStreams)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (!client.IsServiceConnected)
            {
                throw new InvalidOperationException(Strings.ErrorNotConnected);
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (playbackStreams == null)
            {
                throw new ArgumentNullException("playbackStreams");
            }

            // verify stream exists in the file
            using (KStudioEventFile file = client.OpenEventFile(filePath))
            {
                HashSet<Guid> fileStreams = new HashSet<Guid>();
                foreach (KStudioEventStream stream in file.EventStreams)
                {
                    fileStreams.Add(stream.DataTypeId);
                }

                if (!fileStreams.IsSupersetOf(playbackStreams))
                {
                    Guid invalidStream = playbackStreams.First(x => !fileStreams.Contains(x));
                    throw new InvalidOperationException(string.Format(Strings.ErrorPlaybackStreamNotInFile, StreamSupport.ConvertStreamGuidToString(invalidStream)));
                }
            }

            // verify stream is supported for playback by the Kinect sensor
            foreach (Guid stream in playbackStreams)
            {
                KStudioEventStream eventStream = client.GetEventStream(stream, KStudioEventStreamSemanticIds.KinectDefaultSensorConsumer);
                if (!eventStream.IsPlaybackable)
                {
                    throw new InvalidOperationException(string.Format(Strings.ErrorPlaybackStreamNotSupported, StreamSupport.ConvertStreamGuidToString(stream)));
                }
            }
        }
    }
}
