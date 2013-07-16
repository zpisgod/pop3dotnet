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
#if NET45
using System.Threading.Tasks;
#endif

using Pop3.IO;

namespace Pop3.Tests.Support
{
    public abstract class BaseDummyNetworkOperations  : INetworkOperations
    {
        #region INetworkOperations

        #region Public Methods

        public void Open( string hostName, int port ) { }

        public void Open( string hostName, int port, bool useSsl ) { }

        public string Read( )
        {
            return GetData( );
        }

        public void Write( string data ) { }

        public void Close( ) { }
        
        #endregion

     

        #region Dispose-Finalize Pattern

        public void Dispose( ) { }

        #endregion

        #endregion

        #region Private Methods

        protected abstract string GetData( );

        #endregion
    }
}