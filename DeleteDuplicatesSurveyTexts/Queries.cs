using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeleteDuplicatesSurveyTexts
{
    class Queries
    {
        public const string InsertPattern = @"
INSERT SurveyText (Locale, Text, {0}, Survey_Id_Root, SurveyVersion_Id_Root, VariableSet_Id_Root) VALUES ({1}) -- {2}
";

        public const string GetAccountConnections = @"SELECT
 ConnectionString
FROM dbo.Accounts
WHERE IsDeleted = 0";

        public const string CheckDups = @"
SELECT SUM(s.count) - COUNT(*) FROM (
SELECT
  st.Question_Id_No_Answer
 ,st.TextBlock_Id
 ,st.Survey_Id_Congratulation
 ,st.Survey_Id_Footer
 ,st.Survey_Id_Header
 ,st.Variable_Id
 ,st.VariableCharacteristic_Id
 ,st.MatrixRow_Autocomplete_Id
 ,st.MatrixColumn_Autocomplete_Id
 ,st.MatrixQuestionColumn_Id
 ,st.MatrixColumnGroup_DropDownPrompt_Id
 ,st.MatrixColumnGroup_Id_No_Answer
 ,st.MatrixColumnGroup_Title_Id
 ,st.RowRightLabel_Id
 ,st.MatrixQuestionRow_Id
 ,st.MultipleChoiceQuestionChoice_Autocomplete_Id
 ,st.MultipleChoiceQuestionChoice_Id
 ,st.RankOrderQuestionChoice_Autocomplete_Id
 ,st.RankOrderQuestionChoice_Id
 ,st.SingleChoiceQuestionChoice_Autocomplete_Id
 ,st.SingleChoiceQuestionChoice_Id
 ,st.CustomForward_Id
 ,st.Question_Id_Question
 ,st.Question_Id_Hint
 ,st.DropDownQuestionChoice_Autocomplete_Id
 ,st.DropDownQuestionChoice_Id
 ,st.DropDownQuestion_Id
 ,st.OpenQuestion_Id
 ,st.SliderQuestionChoice_Autocomplete_Id
 ,st.SliderQuestionChoice_Id
 ,st.Content_MessageTemplate_Id
 ,st.Subject_MessageTemplate_Id
 ,st.SendEmail_Id_Message
 ,st.SendEmail_Id_Subject
 ,st.ValueAssignment_Id
 ,st.ImplicitAssociationQuestionStimulus_Autocomplete_Id
 ,st.ImplicitAssociationQuestionStimulus_Id
 ,st.ImplicitAssociationQuestionId_DislikeButton
 ,st.ImplicitAssociationQuestionId_LikeButton
 ,st.OpenQuestion_Id_Placeholder
 ,st.Locale
 ,st.Survey_Id_Root
 ,st.SurveyVersion_Id_Root
 ,st.VariableSet_Id_Root
 ,COUNT(*) as count
FROM SurveyText st
WHERE st.Question_Id_No_Answer IS NOT NULL
OR st.TextBlock_Id IS NOT NULL
OR st.Survey_Id_Congratulation IS NOT NULL
OR st.Survey_Id_Footer IS NOT NULL
OR st.Survey_Id_Header IS NOT NULL
OR st.Variable_Id IS NOT NULL
OR st.VariableCharacteristic_Id IS NOT NULL
OR st.MatrixRow_Autocomplete_Id IS NOT NULL
OR st.MatrixColumn_Autocomplete_Id IS NOT NULL
OR st.MatrixQuestionColumn_Id IS NOT NULL
OR st.MatrixColumnGroup_DropDownPrompt_Id IS NOT NULL
OR st.MatrixColumnGroup_Id_No_Answer IS NOT NULL
OR st.MatrixColumnGroup_Title_Id IS NOT NULL
OR st.RowRightLabel_Id IS NOT NULL
OR st.MatrixQuestionRow_Id IS NOT NULL
OR st.MultipleChoiceQuestionChoice_Autocomplete_Id IS NOT NULL
OR st.MultipleChoiceQuestionChoice_Id IS NOT NULL
OR st.RankOrderQuestionChoice_Autocomplete_Id IS NOT NULL
OR st.RankOrderQuestionChoice_Id IS NOT NULL
OR st.SingleChoiceQuestionChoice_Autocomplete_Id IS NOT NULL
OR st.SingleChoiceQuestionChoice_Id IS NOT NULL
OR st.CustomForward_Id IS NOT NULL
OR st.Question_Id_Question IS NOT NULL
OR st.Question_Id_Hint IS NOT NULL
OR st.DropDownQuestionChoice_Autocomplete_Id IS NOT NULL
OR st.DropDownQuestionChoice_Id IS NOT NULL
OR st.DropDownQuestion_Id IS NOT NULL
OR st.OpenQuestion_Id IS NOT NULL
OR st.SliderQuestionChoice_Autocomplete_Id IS NOT NULL
OR st.SliderQuestionChoice_Id IS NOT NULL
OR st.Content_MessageTemplate_Id IS NOT NULL
OR st.Subject_MessageTemplate_Id IS NOT NULL
OR st.SendEmail_Id_Message IS NOT NULL
OR st.SendEmail_Id_Subject IS NOT NULL
OR st.ValueAssignment_Id IS NOT NULL
OR st.ImplicitAssociationQuestionStimulus_Autocomplete_Id IS NOT NULL
OR st.ImplicitAssociationQuestionStimulus_Id IS NOT NULL
OR st.ImplicitAssociationQuestionId_DislikeButton IS NOT NULL
OR st.ImplicitAssociationQuestionId_LikeButton IS NOT NULL
OR st.OpenQuestion_Id_Placeholder IS NOT NULL
GROUP BY st.Question_Id_No_Answer
        ,st.TextBlock_Id
        ,st.Survey_Id_Congratulation
        ,st.Survey_Id_Footer
        ,st.Survey_Id_Header
        ,st.Variable_Id
        ,st.VariableCharacteristic_Id
        ,st.MatrixRow_Autocomplete_Id
        ,st.MatrixColumn_Autocomplete_Id
        ,st.MatrixQuestionColumn_Id
        ,st.MatrixColumnGroup_DropDownPrompt_Id
        ,st.MatrixColumnGroup_Id_No_Answer
        ,st.MatrixColumnGroup_Title_Id
        ,st.RowRightLabel_Id
        ,st.MatrixQuestionRow_Id
        ,st.MultipleChoiceQuestionChoice_Autocomplete_Id
        ,st.MultipleChoiceQuestionChoice_Id
        ,st.RankOrderQuestionChoice_Autocomplete_Id
        ,st.RankOrderQuestionChoice_Id
        ,st.SingleChoiceQuestionChoice_Autocomplete_Id
        ,st.SingleChoiceQuestionChoice_Id
        ,st.CustomForward_Id
        ,st.Question_Id_Question
        ,st.Question_Id_Hint
        ,st.DropDownQuestionChoice_Autocomplete_Id
        ,st.DropDownQuestionChoice_Id
        ,st.DropDownQuestion_Id
        ,st.OpenQuestion_Id
        ,st.SliderQuestionChoice_Autocomplete_Id
        ,st.SliderQuestionChoice_Id
        ,st.Content_MessageTemplate_Id
        ,st.Subject_MessageTemplate_Id
        ,st.SendEmail_Id_Message
        ,st.SendEmail_Id_Subject
        ,st.ValueAssignment_Id
        ,st.ImplicitAssociationQuestionStimulus_Autocomplete_Id
        ,st.ImplicitAssociationQuestionStimulus_Id
        ,st.ImplicitAssociationQuestionId_DislikeButton
        ,st.ImplicitAssociationQuestionId_LikeButton
        ,st.OpenQuestion_Id_Placeholder
        ,st.Survey_Id_Root
        ,st.SurveyVersion_Id_Root
        ,st.VariableSet_Id_Root
        ,st.Survey_Id_Root
        ,st.SurveyVersion_Id_Root
        ,st.VariableSet_Id_Root
        ,st.Locale
HAVING COUNT(*) > 1
) s";


        public const string GetDupItem = @"
SELECT st.{0}
 ,st.Locale
 ,COUNT(*)
FROM SurveyText st
WHERE st.{0} IS NOT NULL
GROUP BY st.{0}
        ,st.Locale
HAVING COUNT(*) > 1";

        public const string GetDupItemExt = @"
SELECT st.{0}
 ,st.Locale
 ,COUNT(*)
FROM SurveyText st
WHERE st.{0} IS NOT NULL AND (st.Survey_Id_Root = {1} OR st.SurveyVersion_Id_Root IN ({2}))
GROUP BY st.{0}
        ,st.Locale
HAVING COUNT(*) > 1
";

        public const string GetSurveyVersions = @"SELECT sv.Id FROM SurveyVersion sv WHERE sv.Survey_Id = {0}";

        public const string GetDupGroupAllItem = @"
SELECT st.Id
 ,st.Text
 ,st.Locale
 ,st.{0}
 ,st.Survey_Id_Root
 ,st.SurveyVersion_Id_Root
 ,st.VariableSet_Id_Root
FROM SurveyText st
WHERE st.{0} = {1} AND st.Locale IN ({2})
ORDER BY Id
";

        public const string DeleteItem = @"
DELETE FROM SurveyText WHERE SurveyText.Id IN ({0});
";

        public const string FindTextItems = @"
SELECT COUNT(*) FROM SurveyText st WHERE st.Id IN ({0});
";

        public static readonly string[] ColumnNames = {
            "Question_Id_No_Answer"
            ,"TextBlock_Id"
            ,"Survey_Id_Congratulation"
            ,"Survey_Id_Footer"
            ,"Survey_Id_Header"
            ,"Variable_Id"
            ,"VariableCharacteristic_Id"
            ,"MatrixRow_Autocomplete_Id"
            ,"MatrixColumn_Autocomplete_Id"
            ,"MatrixQuestionColumn_Id"
            ,"MatrixColumnGroup_DropDownPrompt_Id"
            ,"MatrixColumnGroup_Id_No_Answer"
            ,"MatrixColumnGroup_Title_Id"
            ,"RowRightLabel_Id"
            ,"MatrixQuestionRow_Id"
            ,"MultipleChoiceQuestionChoice_Autocomplete_Id"
            ,"MultipleChoiceQuestionChoice_Id"
            ,"RankOrderQuestionChoice_Autocomplete_Id"
            ,"RankOrderQuestionChoice_Id"
            ,"SingleChoiceQuestionChoice_Autocomplete_Id"
            ,"SingleChoiceQuestionChoice_Id"
            ,"CustomForward_Id"
            ,"Question_Id_Question"
            ,"Question_Id_Hint"
            ,"DropDownQuestionChoice_Autocomplete_Id"
            ,"DropDownQuestionChoice_Id"
            ,"DropDownQuestion_Id"
            ,"OpenQuestion_Id"
            ,"SliderQuestionChoice_Autocomplete_Id"
            ,"SliderQuestionChoice_Id"
            ,"Content_MessageTemplate_Id"
            ,"Subject_MessageTemplate_Id"
            ,"SendEmail_Id_Message"
            ,"SendEmail_Id_Subject"
            ,"ValueAssignment_Id"
            ,"ImplicitAssociationQuestionStimulus_Autocomplete_Id"
            ,"ImplicitAssociationQuestionStimulus_Id"
            ,"ImplicitAssociationQuestionId_DislikeButton"
            ,"ImplicitAssociationQuestionId_LikeButton"
            ,"OpenQuestion_Id_Placeholder"
        };
    }
}
