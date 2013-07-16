/*
 * Author: Rodolfo Finochietti
 * Email: rfinochi@shockbyte.net
 * Web: http://shockbyte.net
 *
 * This work is licensed under the Creative Commons Attribution License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by/2.0
 * or send a letter to Creative Commons, 559 Nathan Abbott Way, Stanford, California 94305, USA.
 * 
 * No warranties expressed or implied, use at your own risk.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Pop3.IO;

namespace Pop3
{
#if NETFX_CORE
    internal class InternalPop3Client : IPop3Client
#else
    public class Pop3Client : IPop3Client
#endif
    {
        #region Private Fields

        private INetworkOperations _networkOperations;

        #endregion

        #region Constructors

#if NETFX_CORE
        public InternalPop3Client( )
        {
            _networkOperations = new StreamSocketNetworkOperations( );
        }
#else
        public Pop3Client( )
        {
            _networkOperations = new TcpClientNetworkOperations( );
        }

        internal Pop3Client( INetworkOperations networkOperations )
        {
            if ( networkOperations == null )
                throw new ArgumentNullException( "networkOperations", "The parameter networkOperation can't be null" );

            _networkOperations = networkOperations;
        }
#endif
        #endregion

        #region Properties

        public bool IsConnected
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

#if !NETFX_CORE
        public void Connect( string server, string userName, string password )
        {
            Connect( server, userName, password, 110, false );
        }

        public void Connect( string server, string userName, string password, bool useSsl )
        {
            Connect( server, userName, password, ( useSsl ? 995 : 110 ), useSsl );
        }

        public void Connect( string server, string userName, string password, int port, bool useSsl )
        {
            if ( this.IsConnected )
                throw new InvalidOperationException( "Pop3 client already connected" );

            _networkOperations.Open( server, port, useSsl );

            string response = _networkOperations.Read( );

            if ( String.IsNullOrEmpty( response ) || response.Substring( 0, 3 ) != "+OK" )
                throw new InvalidOperationException( response );

            SendCommand( String.Format( CultureInfo.InvariantCulture, "USER {0}", userName ) );
            SendCommand( String.Format( CultureInfo.InvariantCulture, "PASS {0}", password ) );

            this.IsConnected = true;
        }

        public void Disconnect( )
        {
            if ( !this.IsConnected )
                return;

            try
            {
                SendCommand( "QUIT" );
                _networkOperations.Close( );
            }
            finally
            {
                this.IsConnected = false;
            }
        }

        public IEnumerable<Pop3Message> List( )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );

            List<Pop3Message> result = new List<Pop3Message>( );

            SendCommand( "LIST" );

            while ( true )
            {
                string response = _networkOperations.Read( );
                if ( response == ".\r\n" )
                    return result.AsEnumerable( );

                Pop3Message message = new Pop3Message( );

                char[] seps = { ' ' };
                string[] values = response.Split( seps );

                message.Number = Int32.Parse( values[ 0 ], CultureInfo.InvariantCulture );
                message.Bytes = Int32.Parse( values[ 1 ], CultureInfo.InvariantCulture );
                message.Retrieved = false;

                result.Add( message );
            }
        }

        public void RetrieveHeader( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );

            if ( message == null )
                throw new ArgumentNullException( "message" );

            SendCommand( "TOP", "0", message );

            while ( true )
            {
                string response = _networkOperations.Read( );
                if ( response == ".\r\n" )
                    break;

                message.RawHeader += response;
            }
        }

        public void RetrieveHeader( IEnumerable<Pop3Message> messages )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );
            if ( messages == null )
                throw new ArgumentNullException( "messages" );

            foreach ( Pop3Message message in messages )
                RetrieveHeader( message );
        }

        public void Retrieve( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );
            if ( message == null )
                throw new ArgumentNullException( "message" );

            SendCommand( "RETR", message );

            while ( true )
            {
                string response = _networkOperations.Read( );
                if ( response == ".\r\n" )
                    break;

                message.RawMessage += response;
            }
            message.Retrieved = true;
        }

        public void Retrieve( IEnumerable<Pop3Message> messages )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );
            if ( messages == null )
                throw new ArgumentNullException( "messages" );

            foreach ( Pop3Message message in messages )
                Retrieve( message );
        }

        public IEnumerable<Pop3Message> ListAndRetrieveHeader( )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );

            IEnumerable<Pop3Message> messages = List( );

            RetrieveHeader( messages );

            return messages;
        }

        public IEnumerable<Pop3Message> ListAndRetrieve( )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );

            IEnumerable<Pop3Message> messages = List( );

            Retrieve( messages );

            return messages;
        }

        public void Delete( Pop3Message message )
        {
            if ( !this.IsConnected )
                throw new InvalidOperationException( "Pop3 client is not connected to host" );
            if ( message == null )
                throw new ArgumentNullException( "message" );

            SendCommand( "DELE", message );
        }
#endif

        #endregion

    

        #region Private Methods

#if !NETFX_CORE
        private void SendCommand( string command, Pop3Message message )
        {
            SendCommand( command, null, message );
        }

        private void SendCommand( string command, string aditionalParameters = null, Pop3Message message = null )
        {
            var request = new StringBuilder( );

            if ( message == null )
                request.AppendFormat( CultureInfo.InvariantCulture, "{0}", command );
            else
                request.AppendFormat( CultureInfo.InvariantCulture, "{0} {1}", command, message.Number );

            if ( !String.IsNullOrEmpty( aditionalParameters ) )
                request.AppendFormat( " {0}", aditionalParameters );

            request.Append( "\r\n" );

            _networkOperations.Write( request.ToString( ) );

            var response = _networkOperations.Read( );

            if ( String.IsNullOrEmpty( response ) || response.Substring( 0, 3 ) != "+OK" )
                throw new InvalidOperationException( response );
        }
#endif

        #endregion

      

        #region Dispose-Finalize Pattern

        public void Dispose( )
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

#if NETFX_CORE
        ~InternalPop3Client( )
#else
        ~Pop3Client( )
#endif
        {
            Dispose( false );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if ( _networkOperations != null )
                {
                    _networkOperations.Close( );
                    _networkOperations.Dispose( );
                    _networkOperations = null;
                }
            }

        }

        #endregion
    }
}