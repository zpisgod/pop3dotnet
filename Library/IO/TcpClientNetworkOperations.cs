﻿/*
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
#if NET45
using System.Threading.Tasks;
#endif

namespace Pop3.IO
{
    internal class TcpClientNetworkOperations : INetworkOperations
    {
        #region Private Fields

        private TcpClient _tcpClient;
        private Stream _stream;

        #endregion

        #region INetworkOperations

        #region Public Methods

        public void Open( string hostName, int port )
        {
            Open( hostName, port, false );
        }
        
        public void Open( string hostName, int port, bool useSsl )
        {
            if ( _tcpClient == null )
            {
                _tcpClient = new TcpClient( );
                _tcpClient.Connect( hostName, port );

                if ( useSsl )
                {
                    _stream = new SslStream( _tcpClient.GetStream( ), false );
                    ( (SslStream)_stream ).AuthenticateAsClient( hostName );
                }
                else
                {
                    _stream = _tcpClient.GetStream( );
                }
            }
        }

        public string Read( )
        {
            if ( _stream == null )
                throw new InvalidOperationException( "The Network Stream is null" );

            byte[] data = new byte[ 1 ];
            StringBuilder sb = new StringBuilder( );
            UTF8Encoding enc = new UTF8Encoding( );

            while ( true )
            {
                int dataLength = _stream.Read( data, 0, 1 );
                if ( dataLength != 1 )
                    break;

                sb.Append( enc.GetString( data, 0, 1 ) );

                if ( data[ 0 ] == '\n' )
                    break;
            }

            return sb.ToString( );
        }

        public void Write( string data )
        {
            if ( _stream == null )
                throw new InvalidOperationException( "The Network Stream is null" );

            UTF8Encoding en = new UTF8Encoding( );
            byte[] writeBuffer = en.GetBytes( data );

            _stream.Write( writeBuffer, 0, writeBuffer.Length );
        }
        
        public void Close( )
        {
            IDisposable disposableStream = _stream as IDisposable;
            if ( disposableStream != null )
                disposableStream.Dispose( );

            if ( _tcpClient != null )
            {
                _tcpClient.Close( );
                _tcpClient = null;
            }                  
        }

        #endregion

     

        #endregion

        #region Dispose-Finalize Pattern

        public void Dispose( )
        {
            Close( );
            
            GC.SuppressFinalize( this );
        }

        #endregion
    }
}