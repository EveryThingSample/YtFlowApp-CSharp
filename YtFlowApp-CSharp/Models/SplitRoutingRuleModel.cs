using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YtFlowApp2.Models
{
    public enum SplitRoutingRuleDecision
    {
        Next = 0,
        Direct = 1,
        Proxy = 2,
        Reject = 3,
    }
    public class SplitRoutingRuleModel : INotifyPropertyChanged
    {
        private string _rule;
        private SplitRoutingRuleDecision _decision;

        public SplitRoutingRuleModel()
        {
        }

        public SplitRoutingRuleModel(string rule, SplitRoutingRuleDecision decision)
        {
            _rule = rule;
            _decision = decision;
        }

        public string Rule
        {
            get => _rule;
            set
            {
                if (_rule != value)
                {
                    _rule = value;
                    OnPropertyChanged(nameof(Rule));
                }
            }
        }

        public SplitRoutingRuleDecision Decision
        {
            get => _decision;
            set
            {
                if (_decision != value)
                {
                    _decision = value;
                    OnPropertyChanged(nameof(Decision));
                    OnPropertyChanged(nameof(DecisionIndex));
                }
            }
        }

        public int DecisionIndex
        {
            get => (int)_decision;
            set => Decision = (SplitRoutingRuleDecision)value;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
