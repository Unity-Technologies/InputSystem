using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.ActionsV2
{
	internal interface IBindingPathCompiler
	{
		CompiledBindingPath Compile(string path, BindingPathLexer lexer);
	}

	internal class DefaultBindingPathCompiler : IBindingPathCompiler
	{
		public CompiledBindingPath Compile(string path, BindingPathParser parser)
		{

		}
	}


	internal struct ErrorMessage
	{
		private string m_Message;
		private int m_Position;

		public ErrorMessage(string message, int position)
		{
			m_Message = message;
			m_Position = position;
		}
	}
	internal class DumbParser
	{
		private int m_Position;
		private List<ErrorMessage> m_Errors = new List<ErrorMessage>();

		public CompiledBindingPath Parse(string path)
		{
			var compiledBindingPath = new CompiledBindingPath();

			m_Position = 0;
			char currentChar;

			while (m_Position < path.Length)
			{
				currentChar = path[m_Position++];

				if (currentChar == '/' && compiledBindingPath.device.value == null)
				{
					ParseLayout(path);
				}
				else
				{
					ParseComponent();
				}
			}

			
			return compiledBindingPath;
		}

		private void ParseComponent()
		{
			
		}

		private void ParseLayout(string path)
		{
			if (path[m_Position] != '<')
			{
				AddError("Layouts must start with '<' character.", m_Position);
				return;
			}


		}

		private void AddError(string message, int position)
		{
			m_Errors.Add(new ErrorMessage(message, position));
		}
	}

	internal class BindingPathParser
	{
		private readonly BindingPathLexer m_Lexer;
		private Token m_Token;

		public BindingPathParser(BindingPathLexer lexer)
		{
			m_Lexer = lexer;
		}

		public CompiledBindingPath Parse()
		{
			var compiledBindingPath = new CompiledBindingPath();

			NextToken();

			while (m_Token.tokenKind != TokenKind.EOF)
			{
				switch (m_Token.tokenKind)
				{
					case TokenKind.StartComponent:
						if (compiledBindingPath.device.value != null)
							m_Lexer.lexerState = LexerState.Control;
						NextToken();
						break;

					case TokenKind.OpenLayout:
						m_Lexer.MoveNext();
						break;

					case TokenKind.CloseLayout:
						break;
				}
			}

			return compiledBindingPath;
		}

		private void NextToken()
		{
			m_Token = m_Lexer.MoveNext() ? m_Lexer.token : new Token(TokenKind.EOF, -1, -1);
		}
	}

	internal enum TokenKind
	{
		EOF,
		StartComponent,
		OpenLayout,
		CloseLayout,
		OpenUsage,
		CloseUsage,
		Wildcard,
		DisplayName
	}

	internal enum LexerState
	{
		Layout,
		Control
	}

	internal class BindingPathLexer
	{
		private readonly string m_Path;
		private char m_Current;
		private char m_Previous;
		private int m_Position;
		private List<LexerMessage> m_Errors;

		public Token token { get; private set; }

		public LexerState lexerState { get; set; }

		public BindingPathLexer(string path)
		{
			m_Path = path;
			m_Errors = new List<LexerMessage>();
		}

		public bool MoveNext()
		{
			if (token.tokenKind == TokenKind.EOF)
				return false;

			switch (lexerState)
			{
				case LexerState.Layout:
					NextTokenForLayout();
					break;

				case LexerState.Control:
					NextTokenForControl();
					break;
			}
			return true;
		}

		private void NextTokenForLayout()
		{
			switch (m_Current)
			{
				case '<':
					token = new Token(TokenKind.OpenLayout, m_Position, m_Position);
					NextChar();
					break;

				case '>':
					token = new Token(TokenKind.CloseLayout, m_Position, m_Position);
					NextChar();
					break;

				case '*':
					token = new Token(TokenKind.Wildcard, m_Position, m_Position);
					break;

				case '/':
					NextChar();

					if (m_Current == '/')
						AddError("Layout cannot be empty. Layout names are enclosed in <>.", m_Position);

					token = new Token(TokenKind.StartComponent, m_Position, m_Position);
					break;
			}
		}

		private void NextTokenForControl()
		{
			
		}

		private void AddError(string message, int position)
		{
			m_Errors = new List<LexerMessage>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void NextChar()
		{
			++m_Position;
			m_Previous = m_Current;
			m_Current = m_Path[m_Position];
		}
	}

	internal struct Token
	{
		private readonly TokenKind m_TokenKind;
		private readonly int m_Start;
		private readonly int m_End;

		public Token(TokenKind tokenKind, int start, int end)
		{
			m_TokenKind = tokenKind;
			m_Start = start;
			m_End = end;
		}

		public TokenKind tokenKind => m_TokenKind;

		public int start => m_Start;

		public int end => m_End;
	}

	internal struct LexerMessage
	{
		public string message { get; }
		public int position { get; }

		public LexerMessage(string message, int position)
		{
			this.message = message;
			this.position = position;
		}
	}
}