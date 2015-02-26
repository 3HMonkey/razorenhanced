using System;
using System.Text;
using System.Collections;

namespace Assistant
{
	internal class ObjectPropertyList
	{
		internal class OPLEntry
		{
			internal int Number = 0;
			internal string Args = null;

			internal OPLEntry(int num)
				: this(num, null)
			{
			}

			internal OPLEntry(int num, string args)
			{
				Number = num;
				Args = args;
			}
		}

		private ArrayList m_StringNums = new ArrayList();

		private int m_Hash = 0;
		private ArrayList m_Content = new ArrayList();
		internal ArrayList Content { get { return m_Content; } }
		
		private int m_CustomHash = 0;
		private ArrayList m_CustomContent = new ArrayList();

		private UOEntity m_Owner = null;

		internal int Hash 
		{ 
			get { return m_Hash ^ m_CustomHash; }
			set { m_Hash = value; }
		}

		internal int ServerHash { get { return m_Hash; } }

		internal bool Customized { get { return m_CustomHash != 0; } }

		internal ObjectPropertyList(UOEntity owner)
		{
			m_Owner = owner;

			m_StringNums.AddRange( m_DefaultStringNums );
		}

		internal void Read(PacketReader p)
		{
			m_Content.Clear();

			p.Seek( 5, System.IO.SeekOrigin.Begin ); // seek to packet data

			p.ReadUInt32(); // serial
			p.ReadByte(); // 0
			p.ReadByte(); // 0
			m_Hash = p.ReadInt32();

			m_StringNums.Clear();
			m_StringNums.AddRange( m_DefaultStringNums );

			while ( p.Position < p.Length )
			{
				int num = p.ReadInt32();
				if ( num == 0 )
					break; 

				m_StringNums.Remove( num );

				short bytes = p.ReadInt16();
				string args = null;
				if ( bytes > 0 )
					args = p.ReadUnicodeStringBE( bytes >> 1 );

				m_Content.Add( new OPLEntry( num, args ) );
			}

			for(int i=0;i<m_CustomContent.Count;i++)
			{
				OPLEntry ent = (OPLEntry)m_CustomContent[i];
				if ( m_StringNums.Contains( ent.Number ) )
				{
					m_StringNums.Remove( ent.Number );
				}
				else
				{
					for (int s=0;s<m_DefaultStringNums.Length;s++)
					{
						if ( ent.Number == m_DefaultStringNums[s] )
						{
							ent.Number = GetStringNumber();
							break;
						}
					}
				}
			}
		}

		internal void Add(int number)
		{
			if ( number == 0 )
				return;

			AddHash( number );

			m_CustomContent.Add( new OPLEntry( number ) );
		}

		private static byte[] m_Buffer = new byte[0];

		internal void AddHash(int val)
		{
			m_CustomHash ^= (val & 0x3FFFFFF);
			m_CustomHash ^= (val >> 26) & 0x3F;
		}

		internal void Add(int number, string arguments)
		{
			if ( number == 0 )
				return;
			
			AddHash( number );
			m_CustomContent.Add( new OPLEntry( number, arguments ) );
		}

		internal void Add(int number, string format, object arg0)
		{
			Add( number, String.Format( format, arg0 ) );
		}

		internal void Add(int number, string format, object arg0, object arg1)
		{
			Add( number, String.Format( format, arg0, arg1 ) );
		}

		internal void Add(int number, string format, object arg0, object arg1, object arg2)
		{
			Add( number, String.Format( format, arg0, arg1, arg2 ) );
		}

		internal void Add(int number, string format, params object[] args)
		{
			Add( number, String.Format( format, args ) );
		}

		private static int[] m_DefaultStringNums = new int[] 
		{ 
			1042971, // ~1_NOTHING~
			1070722, // ~1_NOTHING~
			1063483, // ~1_MATERIAL~ ~2_ITEMNAME~
			1076228, // ~1_DUMMY~ ~2_DUMMY~
			1060847, // ~1_val~ ~2_val~
			1050039, // ~1_NUMBER~ ~2_ITEMNAME~
			// these are ugly:
			//1062613, // "~1_NAME~" (orange)
			//1049644, // [~1_stuff~]
		};

		private int GetStringNumber()
		{
			if ( m_StringNums.Count > 0 )
			{
				int num = (int)m_StringNums[0];
				m_StringNums.RemoveAt( 0 );
				return num;
			}
			else
			{
				return 1049644;
			}
		}

		private const string RazorHTMLFormat = " <CENTER><BASEFONT COLOR=#FF0000>{0}</BASEFONT></CENTER> ";

		internal void Add(string text)
		{
			Add( GetStringNumber(), String.Format( RazorHTMLFormat, text ) );
		}

		internal void Add(string format, string arg0)
		{
			Add( GetStringNumber(), String.Format( format, arg0 ) );
		}

		internal void Add(string format, string arg0, string arg1)
		{
			Add( GetStringNumber(), String.Format( format, arg0, arg1 ) );
		}

		internal void Add(string format, string arg0, string arg1, string arg2)
		{
			Add( GetStringNumber(), String.Format( format, arg0, arg1, arg2 ) );
		}

		internal void Add(string format, params object[] args)
		{
			Add( GetStringNumber(), String.Format( format, args ) );
		}

		internal bool Remove(int number)
		{
			for ( int i = 0; i < m_Content.Count; i++ )
			{
				OPLEntry ent = (OPLEntry)m_Content[i];
				if ( ent == null )
					continue;

				if ( ent.Number == number )
				{
					for (int s=0;s<m_DefaultStringNums.Length;s++)
					{
						if ( m_DefaultStringNums[s] == ent.Number )
						{
							m_StringNums.Insert( 0, ent.Number );
							break;
						}
					}

					m_Content.RemoveAt( i );
					AddHash( ent.Number );
					if ( ent.Args != null && ent.Args != "" )
						AddHash( ent.Args.GetHashCode() );

					return true;
				}
			}

			for ( int i = 0; i < m_CustomContent.Count; i++ )
			{
				OPLEntry ent = (OPLEntry)m_CustomContent[i];
				if ( ent == null )
					continue;

				if ( ent.Number == number )
				{
					for (int s=0;s<m_DefaultStringNums.Length;s++)
					{
						if ( m_DefaultStringNums[s] == ent.Number )
						{
							m_StringNums.Insert( 0, ent.Number );
							break;
						}
					}

					m_CustomContent.RemoveAt( i );
					AddHash( ent.Number );
					if ( ent.Args != null && ent.Args != "" )
						AddHash( ent.Args.GetHashCode() );
					if ( m_CustomContent.Count == 0 )
						m_CustomHash = 0;
					return true;
				}
			}
			return false;
		}

		internal bool Remove(string str)
		{
			string htmlStr = String.Format( RazorHTMLFormat, str );

			/*for ( int i = 0; i < m_Content.Count; i++ )
			{
				OPLEntry ent = (OPLEntry)m_Content[i];
				if ( ent == null )
					continue;

				for (int s=0;s<m_DefaultStringNums.Length;s++)
				{
					if ( ent.Number == m_DefaultStringNums[s] && ( ent.Args == htmlStr || ent.Args == str ) )
					{
						m_StringNums.Insert( 0, ent.Number );

						m_Content.RemoveAt( i );

						AddHash( ent.Number );
						if ( ent.Args != null && ent.Args != "" )
							AddHash( ent.Args.GetHashCode() );
						return true;
					}
				}
			}*/

			for ( int i = 0; i < m_CustomContent.Count; i++ )
			{
				OPLEntry ent = (OPLEntry)m_CustomContent[i];
				if ( ent == null )
					continue;

				for (int s=0;s<m_DefaultStringNums.Length;s++)
				{
					if ( ent.Number == m_DefaultStringNums[s] && ( ent.Args == htmlStr || ent.Args == str ) )
					{
						m_StringNums.Insert( 0, ent.Number );

						m_CustomContent.RemoveAt( i );

						AddHash( ent.Number );
						if ( ent.Args != null && ent.Args != "" )
							AddHash( ent.Args.GetHashCode() );
						return true;
					}
				}
			}

			return false;
		}

		internal Packet BuildPacket()
		{
			Packet p = new Packet( 0xD6 );

			p.EnsureCapacity( 128 );

			p.Write( (short) 0x01 );
			p.Write( (uint) (m_Owner != null ? m_Owner.Serial : Serial.Zero) );
			p.Write( (byte) 0 );
			p.Write( (byte) 0 );
			p.Write( (uint) (m_Hash ^ m_CustomHash) );

			foreach ( OPLEntry ent in m_Content )
			{
				if ( ent != null && ent.Number != 0 )
				{
					p.Write( (int)ent.Number );
					if ( ent.Args != null && ent.Args != "" )
					{
						int byteCount = Encoding.Unicode.GetByteCount( ent.Args );

						if ( byteCount > m_Buffer.Length )
							m_Buffer = new byte[byteCount];

						byteCount = Encoding.Unicode.GetBytes( ent.Args, 0, ent.Args.Length, m_Buffer, 0 );

						p.Write( (short) byteCount );
						p.Write( m_Buffer, 0, byteCount );
					}
					else
					{
						p.Write( (short) 0 );
					}
				}
			}

			foreach ( OPLEntry ent in m_CustomContent )
			{
				try
				{
					if ( ent != null && ent.Number != 0 )
					{
						string arguments = ent.Args;

						p.Write( (int)ent.Number );

						if ( arguments == null || arguments == "" )
							arguments = " ";
						arguments += "\t ";
						
						if ( arguments != null && arguments != "" )
						{
							int byteCount = Encoding.Unicode.GetByteCount( arguments );

							if ( byteCount > m_Buffer.Length )
								m_Buffer = new byte[byteCount];

							byteCount = Encoding.Unicode.GetBytes( arguments, 0, arguments.Length, m_Buffer, 0 );

							p.Write( (short) byteCount );
							p.Write( m_Buffer, 0, byteCount );
						}
						else
						{
							p.Write( (short) 0 );
						}
					}
				}
				catch
				{
				}
			}

			p.Write( (int) 0 );

			return p;
		}
	}

	internal class OPLInfo : Packet
	{
		public OPLInfo( Serial ser, int hash ) : base( 0xDC, 9 )
		{
			Write( (uint) ser );
			Write( (int) hash );
		}
	}
}
