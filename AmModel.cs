using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace CsvToBdf.AMData
{
    public class AMModel
    {
        private List<AMPipe> _pipeModels;
        private List<AMStru> _struModels;
        private List<AMEqui> _equiModels;
        private List<AMStru> _revStruModels;
        public AMModel()
        {
            _pipeModels = new List<AMPipe>();
            _struModels = new List<AMStru>();
            _equiModels = new List<AMEqui>();
            _revStruModels = new List<AMStru>();
        }

        public List<AMPipe> PipeModels
        {
            get { return _pipeModels; }
            set { _pipeModels = value; }
        }
        public List<AMStru> StruModels
        {
            get { return _struModels; }
            set { _struModels = value; }
        }
        public List<AMEqui> EquiModels
        {
            get { return _equiModels; }
            set { _equiModels = value; }
        }
        public List<AMStru> RevStruModels
        {
            get { return _revStruModels; }
            set { _revStruModels = value; }
        }
    }

}
