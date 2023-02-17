using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KSUtil
{
    public class SimpleSocketUDP
    {
        private const int       bufferSize             = 8;
        
        public static string    Message             = null;
        public static byte      MessageByte         = 0;
        
        private Socket          _socket             = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);     
        private State           _state              = new State();
        private EndPoint        _endPointFrom       = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback   _receiveCallback    = null;
        private bool            _debug              = false;

        
        public class State
        {
            public byte[] buffer = new byte[bufferSize];
        }
        

        public SimpleSocketUDP( bool debug = false )
        {
#if DEBUG
            _debug = true;
#else
            _debug = debug;
#endif
        }

        public void CleanUp()
        {
            try
            {                
                 Console.WriteLine( "[{0}] SocketShutdown", Timestamps.TimeCode );
                _socket.Shutdown( SocketShutdown.Both );
            }
            finally
            {
                 Console.WriteLine( "[{0}] Socket Close", Timestamps.TimeCode );
                _socket.Close();
            }

            _socket.Dispose();
        }

        public void Server( string address, int port )
        {
            _socket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true );
            _socket.Bind( new IPEndPoint( IPAddress.Parse( address ), port ) );
            Receive();
        }

        public void Client( string address, int port )
        {
            _socket.Connect( IPAddress.Parse( address ), port );

            Receive();
        }


        public void Send( byte[] data )
        {
            if ( !_socket.Connected )
            {
                Console.WriteLine( "Socket Not connected for SEND" );
                return;
            }

            _socket.BeginSend( data, 0, data.Length, SocketFlags.None, ( ar ) =>
             {
                 // Warning: Errors within here are silent!
                 State so = (State)ar.AsyncState;
                 int bytes = _socket.EndSend(ar);

                 if ( _debug )
                    Console.WriteLine( "[{0}] SEND: {1}", Timestamps.TimeCode, data );
                 
             }, _state );
        }

        public void Send( string text )
        {
            if ( !_socket.Connected )
            {
                Console.WriteLine( "Socket Not connected for SEND" );
                return;
            }

            byte[] data = Encoding.ASCII.GetBytes(text);

            _socket.BeginSend( data, 0, data.Length, SocketFlags.None, ( ar ) =>
             {
                 State so = (State)ar.AsyncState;
                 int numBytes = _socket.EndSend(ar);

                 if ( _debug )
                     Console.WriteLine( "[{0}] SEND: {1}, {2}", Timestamps.TimeCode, numBytes, text );
             }, _state );
        }

        private void Receive()
        {
            _socket.BeginReceiveFrom( _state.buffer, 0, bufferSize, SocketFlags.None, ref _endPointFrom, _receiveCallback = ( ar ) =>
             {
                 try
                 {
                     State so = (State)ar.AsyncState;
                     int numBytes = _socket.EndReceiveFrom(ar, ref _endPointFrom);

#if UDP_MSG_TEXT
                     Message = Encoding.ASCII.GetString( so.buffer, 0, numBytes );
                    
                     if ( _debug )
                         Console.WriteLine( "[{0}] RECV: {1}: {2}, {3}", Timestamps.TimeCode, _endPointFrom.ToString(), numBytes, Message );
#else
                     MessageByte = so.buffer[0];
                     
                     if ( _debug )
                         Console.WriteLine( "[{0}] RECV: {1}: {2}, {3}", Timestamps.TimeCode, _endPointFrom.ToString(), numBytes, MessageByte );
#endif

                     if ( null != _socket )
                         _socket.BeginReceiveFrom( so.buffer, 0, bufferSize, SocketFlags.None, ref _endPointFrom, _receiveCallback, so );
                 }
                 catch ( System.ObjectDisposedException )
                 {
                     Console.WriteLine( "[{0}] RECV: ObjectDisposedException - This is expected.", Timestamps.TimeCode );
                 }                
             }, _state );
        }
    }
}