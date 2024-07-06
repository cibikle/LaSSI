using System;
namespace LaSSI.LassiTools
{
   public class FilterConditionLine
   {
      //FilterLineLogicalOperator logicalOperator = FilterLineLogicalOperator.NULL;
      //string logicalOperator = FilterLogicalOperator.Operators[FilterLineLogicalOperator.NULL];
      string key = "...";
      //public FilterConditionValue value = new("...");
      string filterValue = "...";
      //FilterLineEvaluationOperator evaluationOperator = FilterLineEvaluationOperator.NULL;
      string evaluationOperator = FilterEvaulationOperator.Operators[FilterOperator.Equals];
      string ruleType = FilterRuleType.RuleTypes[FilterRuleTypeEnum.Must];
      //string conditionGroup = "-1";

      //public FilterLineLogicalOperator LogicalOperator
      //{
      //   get { return logicalOperator; }
      //   set { logicalOperator = value; }
      //}
      //public string LogicalOperator
      //{
      //   get { return logicalOperator; }
      //   set { logicalOperator = value; }
      //}
      public string RuleType
      {
         get { return ruleType; }
         set { ruleType = value; }
      }
      public string EvaluationOperator
      {
         get { return evaluationOperator; }
         set { evaluationOperator = value; }
      }
      public string Key
      {
         get { return key; }
         set { key = value; }
      }
      //public string Group
      //{
      //   get { return conditionGroup; }
      //   set { conditionGroup = value; }
      //}
      public string Value
      {
         get { return filterValue; }
         set { filterValue = value; }
      }
      public FilterConditionLine()
      {

      }
      public FilterConditionLine(string key, string value, FilterOperator EvaluationOperator = FilterOperator.Equals, FilterRuleTypeEnum RuleType = FilterRuleTypeEnum.Must)
      {
         this.key = key;
         this.filterValue = value;
         evaluationOperator = FilterEvaulationOperator.Operators[EvaluationOperator];
         ruleType = FilterRuleType.RuleTypes[RuleType];
         //logicalOperator = FilterLogicalOperator.Operators[LogicalOperator];
         //this.conditionGroup = conditionGroup;
      }
   }
}

