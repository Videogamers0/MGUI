using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework;
using MGUI.Shared.Helpers;

namespace MGUI.Core.UI
{
    public class MGPasswordBox : MGTextBox
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private char _PasswordCharacter;
		/// <summary>The character to display for each character in the text. Default value: <b>*</b><para/>
		/// Recommended to use a character that belongs to a common character set, to avoid issues where the <see cref="SpriteFont"/> cannot render the character.</summary>
		public char PasswordCharacter
		{
			get => _PasswordCharacter;
			set
			{
				if (_PasswordCharacter != value)
				{
					_PasswordCharacter = value;
					if (!string.IsNullOrEmpty(Text))
						_ = base.SetText(ReplaceNormalCharactersWith(Text, PasswordCharacter));
					NPC(nameof(PasswordCharacter));
				}
			}
		}

		protected override string GetTextBackingField() => Password ?? string.Empty;
		protected override void SetTextBackingField(string Value)
		{
            _Password?.Dispose();
			_Password = Value.AsSecureString();
			NPC(nameof(Password));
        }

		protected override bool SetText(string Value, bool ExecuteEvenIfSameValue)
		{
            SecureString Temp = Value?.AsSecureString();
            Value = ReplaceNormalCharactersWith(Value, PasswordCharacter);
            if (base.SetText(Value, true))
            {
                //Debug.WriteLine($"{nameof(MGPasswordBox)}: Password changed from: \"{Password}\" to \"{Temp?.AsPlainString()}\"");
                _Password?.Dispose();
                _Password = Temp;
				NPC(nameof(Password));
				PasswordChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Temp?.Dispose();
                return false;
            }
        }

        private static string ReplaceNormalCharactersWith(string s, char c)
		{
			List<char> SpecialCharacters = new() { '\n', '\r' };
			if (s == null)
				return null;
			else
			{
				StringBuilder SB = new(s.Length);
				foreach (char Character in s)
				{
					if (SpecialCharacters.Contains(Character))
						SB.Append(Character);
					else
						SB.Append(c);
				}
				return SB.ToString();
			}
		}

		private SecureString _Password;
		/// <summary>Decodes the <see cref="SecureString"/> holding this <see cref="MGPasswordBox"/>'s Password into a plain-text string and returns the result.</summary>
		public string Password => _Password.AsPlainString();

		public event EventHandler<EventArgs> PasswordChanged;

		/// <summary>This feature is intentionally disabled for <see cref="MGPasswordBox"/></summary>
        public override bool AcceptsReturn { get => false; set { } }
        /// <summary>This feature is intentionally disabled for <see cref="MGPasswordBox"/></summary>
        public override bool AcceptsTab { get => false; set { } }

        public MGPasswordBox(MGWindow Window, int? CharacterLimit = 250)
            : base(Window, MGElementType.PasswordBox, CharacterLimit, false, false)
        {
			using (BeginInitializing())
			{
				this.PasswordCharacter = '*';
				this.AcceptsReturn = false;
				this.AcceptsTab = false;
			}
        }
    }
}
