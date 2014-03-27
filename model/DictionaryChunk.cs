using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PasswordCrackerService.model
{
    [Serializable]
    public class DictionaryChunk
    {
        private List<string> _words;
        private bool _givenAway;

        public List<string> Words
        {
            get { return _words; }
        }

        public bool GivenAway
        {
            get { return _givenAway; }
            set { _givenAway = value; }
        }

        public DictionaryChunk()
        {
            _words = new List<string>();
            _givenAway = false;
        }

        public DictionaryChunk(int capacity)
        {
            _words = new List<string>(capacity);
            _givenAway = false;
        }
    }
}