using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PasswordCrackerService.model
{
    public class DictionaryChunk
    {
        private List<string> _words;
        private bool _processed = false;

        public List<string> Words
        {
            get { return _words; }
        }

        public bool Processed
        {
            get { return _processed; }
            set { _processed = value; }
        }
    }
}