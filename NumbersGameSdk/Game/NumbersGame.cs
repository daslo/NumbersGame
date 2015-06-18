﻿using System;
using System.Collections.Generic;
using System.Linq;

// The Game sub-namespace defines an optional part of the SDK.
// It defines an interface for playing a game, INumbersGame, and provides one standard implementation, NumbersGame.
// The reference algorithms uses the SDK's NumbersGame; faster implementation are possible.
namespace ScottLogic.NumbersGame.Game
{
    /// <summary>
    /// SDK implementation of INumbersGame.
    /// It's a poor class design, by the way. 
    /// Q. Is this class modelling the current state, or remembering the history?
    /// (A. Both)
    /// TODO: Refactor history into separate class
    /// </summary>
    public class NumbersGame : INumbersGame
    {
        // The current state of the game is simply modelled as a list of integers
        // See alternative implementation using an instance-counting Set instead
        private readonly List<int> _numbers = new List<int>();

        // The history is a list of operations
        private readonly List<IOperation> _history = new List<IOperation>();

        // We need to track some implementation details about our operations in order to support UndoOperation.
        private readonly Stack<OperationDetail> _undoDetail = new Stack<OperationDetail>();

        // Cached Operation enum values
        private static readonly Operator[] OperationValues = (Operator[]) Enum.GetValues(typeof (Operator));

        // ---------------------------------------------------------------

        // Main public constructor
        public NumbersGame()
        {
            // Note that the private fields (above) are initialised beforehand
        }

        public NumbersGame(IEnumerable<int> initVals, int target = 0)
            :this()
        {
            foreach (var v in initVals)
            {
                _numbers.Add(v);
            }
            Target = target;
        }

        // Public Copy constructor (deep)
        public NumbersGame(NumbersGame game)
        {
            _numbers.AddRange(game._numbers);
            _history.AddRange(game._history);
            Target = game.Target;
        }

        // Public property for extracting the history when we're done
        // It isn't defined by the interface
        public IEnumerable<IOperation> History
        {
            get { return _history; }
        }

        // ---------------------------
        // INumbersGame implementation
        // ----------------------------
        public void Initialise(int[] initialValues, int target)
        {
            _numbers.Clear();
            _numbers.AddRange(initialValues);
            Target = target;
        }


        public void DoOperation(IOperation op) // int idx1, int idx2, Operator op, out int lhs, out int rhs)
        {
            int idx1 = _numbers.FindIndex(i => i == op.Lhs); // Index of first occurence of Lhs operand

            // BUG FIX!
            int idx2 = (op.Lhs == op.Rhs)
                ? _numbers.FindIndex(idx1+1, i => i == op.Rhs) // Gets the next occurence of the same value
                : _numbers.FindIndex(i => i == op.Rhs);

            // We'll overwrite the value at idx1, and erase the value at idx2
            // If idx2 < idx1, when restoring, we'll be off by one
            _undoDetail.Push(new OperationDetail(idx1, idx2, _numbers[idx1], _numbers[idx2]));

            // Overwrite one
            _numbers[idx1] = op.Result;
            // Remove the other
            _numbers.RemoveAt(idx2);
            // and record what we've done
            _history.Add(op);

            // Note: In order to properly undo an operation, it is essential that _numbers is
            // restored exactly as it was before. The ordering of elements within _numbers IS important, within
            // the context of iterating through all possible pairings and operations.
            
        }


        public void UndoOperation()
        {
            OperationDetail detail = _undoDetail.Pop();

            int lastHistoryIndex = _history.Count - 1;
            _history.RemoveAt(lastHistoryIndex);

            _numbers.Insert(detail.ErasePos, detail.ErasedValue);
            _numbers[detail.OverwritePos] = detail.OverwrittenValue;
            
        }

        public INumbersGame CreateDescendent(IOperation op)
        {
            var game = new NumbersGame(this); // Deep copy
            game.DoOperation(op);
            return game;
        }

        public List<INumbersGame> CreateAllDescendents()
        {
            var games = new List<INumbersGame>();
            for (int i = 0; i < NumberCount - 1; ++i)
            {
                int n1 = _numbers[i];
                for (int j = i + 1; j < NumberCount; ++j)
                {
                    int n2 = _numbers[j];
                    games.AddRange(
                        OperationValues.Where(op => Operation.IsValid(n1, n2, op))
                            .Select(op => CreateDescendent(new Operation(n1, n2, op))));
                }
            }
            return games;
        }
        public bool IsSolved
        {
            get { return _numbers.Contains<int>(Target); }
        }

        public bool IsComplete
        {
            get { return IsSolved || IsExhausted; }
        }

        public bool IsExhausted
        {
            get { return NumberCount < 2; }
        }

        public int NumberCount
        {
            get { return _numbers.Count; }
        }


        public int Target { get; set; }

        public int[] CurrentNumbers
        {
            get { return _numbers.ToArray(); }
        }

        public int GetNumber(int idx)
        {
            return _numbers[idx];
        }


}
}

