namespace GreatSnooper.Services
{
    using System.Collections.Generic;
    using System.Text;
    using GreatSnooper.ServiceInterfaces;

    public class WormNetCharTable : IWormNetCharTable
    {
        #region Fields
        // A table to decode the messages sent from the WormNet servers. The encoding table will be generated from this.
        private readonly char[] _decode =
        {
            ////x0      x1      x2      x3      x4      x5      x6      x7      x8      x9      xA      xB      xC      xD      xE      xF
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07', '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F', // 0x
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F', // 1x
            ' ',    '!',    '"',    '#',    '$',    '%',    '&',    '\'',   '(',    ')',    '*',    '+',    ',',    '-',    '.',    '/',    // 2x
            '0',    '1',    '2',    '3',    '4',    '5',    '6',    '7',    '8',    '9',    ':',    ';',    '<',    '=',    '>',    '?',    // 3x
            '@',    'A',    'B',    'C',    'D',    'E',    'F',    'G',    'H',    'I',    'J',    'K',    'L',    'M',    'N',    'O',    // 4x
            'P',    'Q',    'R',    'S',    'T',    'U',    'V',    'W',    'X',    'Y',    'Z',    '[',    '\\',   ']',    '^',    '_',    // 5x
            '`',    'a',    'b',    'c',    'd',    'e',    'f',    'g',    'h',    'i',    'j',    'k',    'l',    'm',    'n',    'o',    // 6x
            'p',    'q',    'r',    's',    't',    'u',    'v',    'w',    'x',    'y',    'z',    '{',    '|',    '}',    '~',    '\0',   // 7x
            'Б',    'Г',    'Д',    'Ж',    'З',    'И',    'Й',    'К',    'Л',    'П',    'У',    'Ф',    'Ц',    'Ч',    'Ш',    'Щ',    // 8x
            'Ъ',    'Ы',    'Ь',    'Э',    'Ю',    '\0',   'Я',    'б',    'в',    'г',    'д',    'ж',    'з',    'и',    'й',    'Ÿ',    // 9x
            '\b',   '¡',    'к',    '£',    '\0',   'л',    'м',    'н',    'п',    'т',    'ф',    'ц',    'ч',    'ш',    'щ',    'ъ',    // Ax
            'ы',    'ь',    'э',    'ю',    'я',    'Ő',    'ő',    'Ű',    'ű',    '\0',   '\0',   '\0',   '\0',   '\0',   '\0',   '¿',    // Bx
            'À',    'Á',    'Â',    'Ã',    'Ä',    'Å',    'Æ',    'Ç',    'È',    'É',    'Ê',    'Ë',    'Ì',    'Í',    'Î',    'Ï',    // Cx
            'Ð',    'Ñ',    'Ò',    'Ó',    'Ô',    'Õ',    'Ö',    '×',    'Ø',    'Ù',    'Ú',    'Û',    'Ü',    'Ý',    'Þ',    'ß',    // Dx
            'à',    'á',    'â',    'ã',    'ä',    'å',    'æ',    'ç',    'è',    'é',    'ê',    'ë',    'ì',    'í',    'î',    'ï',    // Ex
            'ð',    'ñ',    'ò',    'ó',    'ô',    'õ',    'ö',    '÷',    'ø',    'ù',    'ú',    'û',    'ü',    'ý',    'þ',    'ÿ'     // Fx
        };
        private readonly char[] _decodeGame = new char[256];

        // A table to encode messages to send them to the WormNet (will be generated in the constructor)
        private readonly Dictionary<char, byte> _encode = new Dictionary<char, byte>();
        private readonly Dictionary<char, byte> _encodeGame = new Dictionary<char, byte>();
        #endregion

        // This method ensures that the initialization will be made from the appropriate thread
        public WormNetCharTable()
        {
            for (int i = 0; i < _decode.Length; i++)
            {
                // Generate the encode dictionary
                if (!_encode.ContainsKey(_decode[i]))
                {
                    _encode.Add(_decode[i], System.Convert.ToByte(i));    // We mix up the key - value pairs
                }

                // Generate the decode array for games
                _decodeGame[i] = _decode[i];
            }

            AddCyrillSupport(_encode);

            // <Deadcode> the five characters "&'<>\ are mapped to %10%11%12%13%14%15
            _decodeGame[0x10] = '"';
            _decodeGame[0x11] = '&';
            _decodeGame[0x12] = '\'';
            _decodeGame[0x13] = '<';
            _decodeGame[0x14] = '>';
            _decodeGame[0x15] = '\\';

            // Generate the encode dictionary for games
            for (int i = 0; i < _decodeGame.Length; i++)
            {
                // Generate the encode dictionary for games (WormNet GameList.asp, etc. can't use ; character!)
                if (_decodeGame[i] != ';' && !_encodeGame.ContainsKey(_decodeGame[i]))
                {
                    _encodeGame.Add(_decodeGame[i], System.Convert.ToByte(i));    // We mix up the key - value pairs
                }
            }

            AddCyrillSupport(_encodeGame);
        }

        private void AddCyrillSupport(Dictionary<char, byte> obj)
        {
            // Add missing cyrill letters to the Encode dictionary
            obj.Add('А', System.Convert.ToByte(0x41));
            obj.Add('В', System.Convert.ToByte(0x42));
            obj.Add('С', System.Convert.ToByte(0x43));
            obj.Add('Е', System.Convert.ToByte(0x45));
            obj.Add('Н', System.Convert.ToByte(0x48));
            obj.Add('М', System.Convert.ToByte(0x4D));
            obj.Add('О', System.Convert.ToByte(0x4F));
            obj.Add('Р', System.Convert.ToByte(0x50));
            obj.Add('Т', System.Convert.ToByte(0x54));
            obj.Add('Х', System.Convert.ToByte(0x58));
            obj.Add('а', System.Convert.ToByte(0x61));
            obj.Add('с', System.Convert.ToByte(0x63));
            obj.Add('е', System.Convert.ToByte(0x65));
            obj.Add('о', System.Convert.ToByte(0x6F));
            obj.Add('р', System.Convert.ToByte(0x70));
            obj.Add('х', System.Convert.ToByte(0x78));
            obj.Add('у', System.Convert.ToByte(0x79));
            obj.Add('Ё', System.Convert.ToByte(0xCB));
            obj.Add('ё', System.Convert.ToByte(0xEB));
        }

        // Remove non-wormnet characters from a string
        public string Encode(string input)
        {
            return RemoveWrongChars(input, _encode);
        }

        public string EncodeGame(string input)
        {
            return RemoveWrongChars(input, _encodeGame);
        }

        public byte GetByteForChar(char c)
        {
            return _encode[c];
        }

        public string EncodeGameUrl(string input)
        {
            StringBuilder sb = new StringBuilder(input);
            for (int i = 0; i < sb.Length; i++)
            {
                char ch = sb[i];
                if (ch == '"' || ch == '&' || ch == '\'' || ch == '<' || ch == '>' || ch == '\\')
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%" + _encodeGame[ch].ToString("X"));
                    i += 2;
                }
                else if (ch == '#' || ch == '+' || ch == '%')
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%" + _encodeGame[ch].ToString("X"));
                    i += 2;
                }
                else if (ch == ' ')
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%A0");
                    i += 2;
                }
                else if (_encodeGame[ch] >= 0x80)
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%" + _encodeGame[ch].ToString("X"));
                    i += 2;
                }
            }
            return sb.ToString();
        }

        private string RemoveWrongChars(string input, Dictionary<char, byte> map)
        {
            StringBuilder sb = new StringBuilder(input);
            for (int i = 0; i < sb.Length; i++)
            {
                if (!map.ContainsKey(sb[i]))
                {
                    sb.Remove(i, 1);
                    i--;
                }
            }
            return sb.ToString().TrimEnd();
        }
    }
}