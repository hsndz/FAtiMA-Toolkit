﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using CommeillFaut.DTOs;
using Conditions;
using Conditions.DTOs;
using SerializationUtilities;
using WellFormedNames;
using EmotionalAppraisal;
using GAIPS.Rage;
using KnowledgeBase;
using WellFormedNames.Collections;


namespace CommeillFaut
{
       [Serializable]
    public sealed class CommeillFautAsset  : LoadableAsset<CommeillFautAsset>, ICustomSerialization
    {

        public KB m_kB;
        public List<SocialExchange> m_SocialExchanges { get; set; }
        public Dictionary<string, string[]> ConditionList;
        public TriggerRules _TriggerRules;
        private List<Name> _actorsList;

        //Volatile Statements
        private NameSearchTree<NameSearchTree<float>> m_cachedCIF = new NameSearchTree<NameSearchTree<float>>();

        /// <summary>
        /// The Comme ill Faut constructor
        /// </summary>

        public KB LinkedEA
        {
            get { return m_kB; }
        }

        public CommeillFautAsset()
        {
            m_kB = null;
            m_SocialExchanges = new List<SocialExchange>();
            ConditionList = new Dictionary<string, string[]>();
            m_cachedCIF = new NameSearchTree<NameSearchTree<float>>();
            _TriggerRules = new TriggerRules();
            _actorsList = new List<Name>();
        }

        /// <summary>
        /// Binds a KB to this Comme ill Faut Asset instance. Without a KB instance binded to this asset, 
        /// comme ill faut evaluations will not work properly.
        /// InvalidateCachedCIF() is automatically called by this method.
        /// </summary>
        /// <param name="kb">The Knowledge Base to be binded to this asset.</param>
        public void RegisterKnowledgeBase(KB kB)
        {
            if (m_kB != null)
            {
                //Unregist bindings
                UnbindToRegistry(m_kB);
                m_kB = null;
            }

            m_kB = kB;
            BindToRegistry(kB);
            InvalidateCachedCIF();
        }

        #region Dynamic Properties

        public void BindToRegistry(IDynamicPropertiesRegistry registry)
        {
            //       registry.RegistDynamicProperty(CIF_DYNAMIC_PROPERTY_NAME, CIFPropertyCalculator);
       
            registry.RegistDynamicProperty(VOLITION_PROPERTY_TEMPLATE, VolitionPropertyCalculator);
        }

       

        private static readonly Name VOLITION_PROPERTY_TEMPLATE = (Name)"Volition";

        public IEnumerable<DynamicPropertyResult> VolitionPropertyCalculator(IQueryContext context, Name socialMoveName, Name initator, Name Target)
        {
       //   Console.WriteLine("VolitionProperty " + socialMoveName.ToString());


            foreach (var t in context.AskPossibleProperties(Target))
            {
           //    Console.WriteLine("Target: " + t.Item1 + "  Original target: " + Target + " found size: " + context.AskPossibleProperties(Target).Count());

                if (m_SocialExchanges.Find(x => x.ActionName == socialMoveName) != null)
                {
                    var value = CalculateVolitions(socialMoveName.ToString(), t.Item1.ToString(),
                        context.Perspective.ToString());

                    var sub =
                        new SubstitutionSet(new Substitution[]
                            {new Substitution(Name.BuildName("[x]"), t.Item1)});

                    Console.WriteLine(" Result: " + "Volition(" + socialMoveName + "," + initator + "," + t.Item1.ToString() + ")" + "=" +
                                      value);
                    yield return new DynamicPropertyResult(Name.BuildName(value), sub);

                }
                else
                {
                    Console.WriteLine("No such social exchange with that name found \n");
                    yield break;

                }
            }
        }
        
        

        public void UnbindToRegistry(IDynamicPropertiesRegistry registry)
        {
            registry.UnregistDynamicProperty((Name)"Volition");
        }

        #endregion

        private void ValidateEALink()
        {
            if (m_kB == null)
                throw new InvalidOperationException($"Cannot execute operation as an instance of {nameof(CommeillFautAsset)} was not registed in this asset.");
        }


        /// <summary>
        /// Calculate the Volition value of a given target, in a particular perspective.
        /// If no perspective is given, the current agent's perspective is used as default.
        /// </summary>
        /// <remarks>
        /// All values calculated by this method are automatically cached, in order to optimize future searches.
        /// If the values are needed to be recalculated, call InvalidateCachedSI() to clear the cached values.
        /// </remarks>
        /// <param name="target">The name of target which we want to calculate the SI</param>
        /// <param name="perspective">From which perspective do we want to calculate de SI.</param>
        /// <returns>The value of Social Importance attributed to given target by the perspective of a particular agent.</returns>
        public string GetSocialVolition(string target, string perspective = "self")
        {
            ValidateEALink();

            var t = Name.BuildName(target);
            if (!t.IsPrimitive)
                throw new ArgumentException("must be a primitive name", nameof(target));

            var p = m_kB.AssertPerspective(Name.BuildName(perspective));

            return internal_GetSocialVolition(t, p);
        }

        private string internal_GetSocialVolition(Name target, Name perspective)
        {
            Console.WriteLine("internal Get social Volition");
            NameSearchTree<float> targetDict;
            string ret_value = "";
            if (!m_cachedCIF.TryGetValue(perspective, out targetDict))
            {
                targetDict = new NameSearchTree<float>();
                m_cachedCIF[perspective] = targetDict;
            }

          
            float value;
            if (!targetDict.TryGetValue(target, out value))
            {
             
                var action = CalculateSocialMove(target.ToString(), perspective.ToString());
                ret_value = action.ActionName.ToString();
               
                targetDict[target] = value;
            }
         
            Console.WriteLine("retvalue: " + ret_value + " target " + target + " perpective " + perspective);
            return ret_value;
        }


        /// <summary>
        /// Clears all cached Social Importance values, allowing new values to be recalculated uppon request.
        /// </summary>
        public void InvalidateCachedCIF()
        {
            m_cachedCIF.Clear();
        }




        public void CheckTriggerRules()
        {
            _TriggerRules.Verify(this.m_kB);
        }

      /*  private List<Name> getTargetList()
        {
            List<Name> retList = new List<Name>();
     

            var total = m_kB.AskProperty(Name.BuildName("NumberofTargets(" + "SELF" + ")"), m_kB.Perspective);

            for (int i = Int32.Parse(total.ToString()); i>0; i--)
            {
                var target = m_kB.AskProperty(Name.BuildName("Target(" + i + ")"), m_kB.Perspective);
                retList.Add(target);
            }

            _actorsList = retList;
           
            return retList;
        }
        */
        public Guid AddExchange(SocialExchangeDTO newExchange)
        {
            var newSocialExchange = new SocialExchange(newExchange);

            
      
            if(m_SocialExchanges != null)
              {

              // m_SocialExchanges = new List<SocialExchange>();
            if(m_SocialExchanges.Find(x => x.ActionName.ToString() == newExchange.Action) != null)
                    UpdateSocialExchange(newExchange);

                   else m_SocialExchanges.Add(newSocialExchange);

               
            }
            return new Guid();
        }


        public Guid AddTriggerRule(InfluenceRuleDTO rule, string cond)
        {

            return _TriggerRules.AddTriggerRule(rule, cond);
        }
        /// <summary>
        /// Updates a reaction definition.
        /// </summary>
        public void UpdateSocialExchange(SocialExchangeDTO reactionToEdit, SocialExchangeDTO newReaction)
        {
            m_SocialExchanges.Remove(new SocialExchange(reactionToEdit));

            var newId = this.AddExchange(newReaction);

           
            
         
        }

        public void UpdateSocialExchange(SocialExchangeDTO newReaction)
        {

          

            m_SocialExchanges.Remove(m_SocialExchanges.Find(x => x.ActionName.ToString() == newReaction.Action));

            m_SocialExchanges.Add(new SocialExchange(newReaction));
        }


        public void UpdateTriggerRule(InfluenceRuleDTO rule, string cond)
        {
           _TriggerRules.UpdateTriggerRule(rule, cond);
        }


        public void RemoveSocialExchanges(IList<Guid> toRemove)
        {
            foreach (var id in toRemove)
            {
                m_SocialExchanges.Remove(m_SocialExchanges.Find(x => new Guid() == id));
            }
        }


        public void RemoveSocialExchange(SocialExchange torem)
        {
          
                m_SocialExchanges.Remove(torem);
           
        }

        public void RemoveTriggerRule(InfluenceRuleDTO rule)
        {

            _TriggerRules.RemoveTriggerRule(rule);
        }

        public void RemoveTriggerRuleByName(string ruleName)
        {
            
            _TriggerRules.RemoveTriggerRuleByName(ruleName);
        }

        public SocialExchange GetSocialMove(Name socialExchangeName)
        {
          return  m_SocialExchanges.Find(x => x.ActionName == socialExchangeName);
        }

        public SocialExchange GetHighestVolition(Dictionary<string, int> _volitions)
        {

            int index = 0;
            var first = _volitions.First();
            string key = first.Key;
            int compareValue = _volitions[key];
            

            foreach (var aux in _volitions.Keys)
            {
                if (compareValue < _volitions[aux])
                {
                    compareValue = _volitions[aux];
                    key = aux;
                }
            }
           
           
            return m_SocialExchanges.Find(x =>x.ActionName.ToString() == key);

        }

      

        public Dictionary<string, int> CalculateSocialMovesVolitions(string target, string perspective)
        {

            
            Dictionary<string,int> volitions = new Dictionary<string, int>();

            foreach (var socialMove in m_SocialExchanges)
            {
                int volitionResult = socialMove.CalculateVolition(perspective, target, this.m_kB);
                volitions.Add(socialMove.ActionName.ToString(), volitionResult);
                Console.WriteLine(" Name " + socialMove.ActionName + " volResult: " + volitionResult);

            }
            return volitions;
        }


        public SocialExchange CalculateSocialMove(string target, string perpective)
        {

            return GetHighestVolition(CalculateSocialMovesVolitions(target, perpective));
        }


        public int CalculateVolitions(string socialMove, string target, string perpective)
        {

            return m_SocialExchanges.Find(x => x.ActionName.ToString() == socialMove)
                .CalculateVolition(perpective, target, this.m_kB);
        }

        public void AddToActorList(Name actor)
        {
           


         var number =   m_kB.AskProperty(Name.BuildName("NumberOfTargets"), m_kB.Perspective);

            if (number == null) number = Name.BuildName(0);
           
            var value = Int32.Parse(number.ToString()) + 1;
            m_kB.Tell(Name.BuildName("NumberOfTargets"), Name.BuildName(value), m_kB.Perspective);
            m_kB.Tell(Name.BuildName("Target(" + value + ")"), actor, m_kB.Perspective);



        }


        public List<Name> getActorList()
        {
            return _actorsList;
        }

        #region Serialization

        public void GetObjectData(ISerializationData dataHolder, ISerializationContext context)
        {
               dataHolder.SetValue("SocialExchanges", m_SocialExchanges.ToArray());
            ConditionList = new Dictionary<string, string[]>();

            foreach (var social in m_SocialExchanges)
            {
                foreach (var rule in social.InfluenceRules)
                {
                    if(rule.RuleConditions?.ConditionSet?.Length > 0)
                        ConditionList.Add(rule.RuleName, rule.RuleConditions.ConditionSet);
                }
            }

            dataHolder.SetValue("RuleList", ConditionList);
            dataHolder.SetValue("_triggerRules", _TriggerRules);

        }

        public void SetObjectData(ISerializationData dataHolder, ISerializationContext context)
        {
            m_SocialExchanges = new List<SocialExchange>(dataHolder.GetValue<SocialExchange[]>("SocialExchanges"));
           ConditionList = dataHolder.GetValue<Dictionary<string,string[]>>("RuleList");

            foreach (var social in m_SocialExchanges)
            {
                foreach (var rule in social.InfluenceRules)
                {
                    if (ConditionList.ContainsKey(rule.RuleName))
                        rule.RuleConditions.ConditionSet = ConditionList[rule.RuleName];
                }
                
            }
            _TriggerRules = dataHolder.GetValue<TriggerRules>("_triggerRules");
            m_cachedCIF = new NameSearchTree<NameSearchTree<float>>();
        }


        #endregion

        public void AppraiseEvents(IEnumerable<Name> eventNames, KB kb)
        {
            foreach (var e in eventNames.Select(e => e.RemoveSelfPerspective(kb.Perspective)))
            {

             
            }
        }

        public void StartSE(Name SE, KB kb)
        {

           
            if (SE.GetNTerm(3).ToString().Contains("SE("))
            {

                var action = SE.GetNTerm(3).ToString();

                char[] delims = {',', '(', ')'};

                var result = action.Split(delims);

                var initiator = SE.GetNTerm(2);
                var target = SE.GetNTerm(4);
                var seName = result[4];
                var SocialExchange = m_SocialExchanges.Find(x => x.ActionName.ToString() == seName);

                Console.WriteLine("CIF Asset, Character: " + initiator + " initiates " + seName + " towards " +
                                     target + "\n");


            }

           
        }

        public void EndSE(Name SE, KB kb)
        {
          
        }

        /// <summary>
        /// Load a Social Importance Asset definition from a DTO object.
        /// </summary>
        /// <remarks>
        /// Use this to procedurally configure the asset.
        /// </remarks>
        /// <param name="dto">
        /// The DTO containing the data to load
        /// </param>
        public void LoadFromDTO(CommeillFautDTO dto)
        {
            m_SocialExchanges.Clear();
            _actorsList = new List<Name>();

            if (dto._SocialExchangesDtos != null)
            {
                foreach (var c in dto._SocialExchangesDtos)
                {
                 m_SocialExchanges.Add(new SocialExchange(c));
                }
            }

            _TriggerRules = dto._triggerRules;


        }

        /// <summary>
        /// Returns a DTO containing all the asset's configurations.
        /// </summary>
        public CommeillFautDTO GetDTO()
        {
            var at = m_SocialExchanges.Select(a => a.ToDTO()).ToArray();
          
            return new CommeillFautDTO() { _SocialExchangesDtos = at, _triggerRules = _TriggerRules};
        }

   

        /// @cond DOXYGEN_SHOULD_SKIP_THIS

        protected override string OnAssetLoaded()
        {
            return null;
        }

        /// @endcond
    }

}

