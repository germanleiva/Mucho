// using System;
// using System.Linq;
// using UnityEngine;
//
// /**
//  * Parsing utils: Converts ConditionData into a predicate function that can be evaluated against a Frame.
//  */
// public static class ConditionMapper
// {
//     public static Func<Frame, bool> BuildPredicate(ConditionData data, IObjectResolver resolver)
//     {
//         if (data == null || data.clauses == null || data.clauses.Count == 0)
//         {
//             return _ => true;
//         }
//
//         return frame =>
//         {
//             var results = data.clauses.Select(c => EvaluateClause(c, frame, resolver));
//
//             return data.op == ConditionOperator.And
//                 ? results.All(x => x)
//                 : results.Any(x => x);
//         };
//     }
//
//     private static bool EvaluateClause(ConditionClauseData clause, Frame frame, IObjectResolver resolver)
//     {
//         switch (clause.type)
//         {
//             case ConditionType.LeftHandGestureIs:
//                 return frame.leftHandGesture.ToString() == clause.leftHandGesture;
//
//             case ConditionType.RightHandGestureIs:
//                 return frame.rightHandGesture.ToString() == clause.rightHandGesture;
//
//             case ConditionType.VoiceCommandIs:
//                 return string.Equals(frame.voiceCommand, clause.voiceCommand, StringComparison.OrdinalIgnoreCase);
//
//             case ConditionType.CollisionBetween:
//             {
//                 GameObject a = resolver.Resolve(clause.objectAId);
//                 GameObject b = resolver.Resolve(clause.objectBId);
//
//                 if (a == null || b == null)
//                     return false;
//
//                 return frame.IsColliding(a, b);
//             }
//
//             default:
//                 return false;
//         }
//     }
// }