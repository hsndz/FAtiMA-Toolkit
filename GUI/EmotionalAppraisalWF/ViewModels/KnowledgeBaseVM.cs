﻿using System;
using System.Collections.Generic;
using System.Linq;
using EmotionalAppraisal;
using EmotionalAppraisal.DTOs;
using EmotionalAppraisalWF.Properties;
using Equin.ApplicationFramework;
using KnowledgeBase;

namespace EmotionalAppraisalWF.ViewModels
{
    public class KnowledgeBaseVM
    {
        private EmotionalAppraisalAsset _emotionalAppraisalAsset;

        public BindingListView<BeliefDTO> Beliefs {get;}
        
        public KnowledgeBaseVM(EmotionalAppraisalAsset ea)
        {
            _emotionalAppraisalAsset = ea;
            var beliefList = ea.Kb.GetAllBeliefs().Select(b => new BeliefDTO
            {
                Name = b.Name.ToString(),
                Value = b.Value.ToString()
            }).ToList();

            this.Beliefs = new BindingListView<BeliefDTO>(beliefList);
        }

        public string[] GetKnowledgeVisibilities()
        {
            return new string[0];// Enum.GetNames(typeof(KnowledgeVisibility));
        }

        public void AddBelief(BeliefDTO belief)
        {
            if (_emotionalAppraisalAsset.BeliefExists(belief.Name))
            {
                throw new Exception(Resources.BeliefAlreadyExistsExceptionMessage);
            }
            _emotionalAppraisalAsset.AddOrUpdateBelief(belief);
            this.Beliefs.DataSource.Add(belief);
            this.Beliefs.Refresh();
        }

        public void RemoveBeliefs(IEnumerable<BeliefDTO> beliefs)
        {
            foreach (var beliefDto in beliefs)
            {
                _emotionalAppraisalAsset.RemoveBelief(beliefDto.Name);
                Beliefs.DataSource.Remove(beliefDto);
            }
            Beliefs.Refresh();
        }
    }
}
