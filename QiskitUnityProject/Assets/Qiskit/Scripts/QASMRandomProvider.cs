﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Qiskit {
    public class QASMRandomProvider : MonoBehaviour {

        /*
         * Optimization: the reinterpret casts (bitwise) and other features
         * could be optimized using StructLayout.
         * 
         * https://docs.microsoft.com/es-es/dotnet/api/system.runtime.interopservices.structlayoutattribute?view=netframework-4.8
         * 
         * Example:
         * [StructLayout(LayoutKind.Explicit)]
         * private struct IntFloat {
         *     [FieldOffset(0)]
         *     public int IntValue;
         *     [FieldOffset(0)]
         *     public float FloatValue;
         * }
         * 
         * .....
         * IntFloat intFloat = new IntFloat { FloatValue = 3.8f };
         * int floatAsInt = intFloat.IntValue;
         * .....
         * 
         */

        private static readonly string _qasmSingleBoolCode = "include \"qelib1.inc\"; qreg q[1]; creg c[1]; h q[0]; measure q[0] -> c[0];";
        //private static string _qasmFourBitCode = "";

        [Header("Optional")]
        public QASMSession specificSession = null;

        private QASMSession executionSession => specificSession ?? QASMSession.instance;

        //public delegate void OnRandomGenerated<T>(T generated);

        //public delegate void OnRandomPoolGenerated<T>(List<T> pool);

        #region Single Value Generation Methods

        /// <summary>
        /// Generates a true random boolean.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomBoolGenerated"/>
        /// </summary>
        /// <param name="onRandomBoolGenerated">The callback called when the bool is available</param>
        public void GenerateBool(Action<bool> onRandomBoolGenerated) {
            // For bool values should be an even number of shots
            QASMExecutable qasmExe = new QASMExecutable(_qasmSingleBoolCode, 15);

            executionSession.ExecuteCode(qasmExe, (response) => {
                onRandomBoolGenerated(response.maxKey == 1);
            });
        }

        /// <summary>
        /// Generates a true random byte.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomByteGenerated"/>
        /// </summary>
        /// <param name="onRandomByteGenerated">The callback called when the byte is available</param>
        public void GenerateByte(Action<byte> onRandomByteGenerated) {
            GenerateIntNbits(8, (i) => onRandomByteGenerated((byte)i));
        }

        /// <summary>
        /// Generates a true random 16 bit int.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomIntGenerated"/>
        /// </summary>
        /// <param name="onRandomIntGenerated">The callback called when the int is available</param>
        public void GenerateInt16(Action<int> onRandomIntGenerated) {
            GenerateIntNbits(16, onRandomIntGenerated);
        }

        /// <summary>
        /// Generates a true random 32 bit int.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomIntGenerated"/>
        /// </summary>
        /// <param name="onRandomIntGenerated">The callback called when the int is available</param>
        public void GenerateInt32(Action<int> onRandomIntGenerated) {
            GenerateIntNbits(32, onRandomIntGenerated);
        }

        /// <summary>
        /// Generates a true random float in the range [0, 1].
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomFloatGenerated"/>
        /// </summary>
        /// <param name="onRandomFloatGenerated">The callback called when the float is available</param>
        public void GenerateFloat(Action<float> onRandomFloatGenerated) {
            GenerateInt32((i) => {
                onRandomFloatGenerated(Int32ToFloat(i));
            });
        }

        /// <summary>
        /// Generates a true random float in the range [<paramref name="min"/>, <paramref name="max"/>].
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomFloatGenerated"/>
        /// </summary>
        /// <param name="min">The smallest number generated</param>
        /// <param name="max">The largest number generated</param>
        /// <param name="onRandomFloatGenerated">The callback called when the float is available</param>
        public void GenerateFloatInRange(float min, float max, Action<float> onRandomFloatGenerated) {
            GenerateInt32((i) => {
                onRandomFloatGenerated(Int32ToFloat(i, min, max));
            });
        }

        /// <summary>
        /// Generates a true random int of n <paramref name="bits"/>.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomIntGenerated"/>
        /// </summary>
        /// <param name="bits">The number of bits used to generate the int</param>
        /// <param name="onRandomFloatGenerated">The callback called when the int is available</param>
        public void GenerateIntNbits(int bits, Action<int> onRandomIntGenerated) {
            executionSession.RequestBackendConfig((backendConfig) => {
                int codeRegs = Mathf.Min(backendConfig.qubitsCount, bits);
                int shotsNeeded = Mathf.CeilToInt((float)bits / codeRegs);
                QASMExecutable qasmExe = new QASMExecutable(RandomNRegisterCode(codeRegs), shotsNeeded);

                executionSession.ExecuteCodeRawResult(qasmExe, (response) => {
                    int rng = 0;
                    for (int i = 0; i < response.rawResult.Count; i++) {
                        rng += response.rawResult[i] << (i * codeRegs);
                    }
                    rng = ClampToBits(rng, bits);
                    onRandomIntGenerated(rng);
                });
            });
        }

        #endregion

        #region Pool Of Values Generation Methods

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random booleans.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomBoolPoolGenerated"/>.
        /// </summary>
        /// <param name="count">The amount of booleans generated</param>
        /// <param name="onRandomBoolPoolGenerated">The callback called when the pool is available</param>
        public void GenerateBoolPool(int count, Action<List<bool>> onRandomBoolPoolGenerated) {
            QASMExecutable qasmExe = new QASMExecutable(_qasmSingleBoolCode, count);

            executionSession.ExecuteCodeRawResult(qasmExe, (response) => {
                List<bool> pool = new List<bool>();
                for (int i = 0; i < response.rawResult.Count; i++) {
                    pool.Add(response.rawResult[i] == 1);
                }
                onRandomBoolPoolGenerated(pool);
            });
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random bytes.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomBytePoolGenerated"/>.
        /// </summary>
        /// <param name="count">The amount of bytes generated</param>
        /// <param name="onRandomBytePoolGenerated">The callback called when the pool is available</param>
        public void GenerateBytePool(int count, Action<List<byte>> onRandomBytePoolGenerated) {
            GenerateIntNbitsPool(8, count, (intPool) => {
                // cast from int to byte
                List<byte> bytePool = new List<byte>();
                foreach (int i in intPool) {
                    bytePool.Add((byte)i);
                }
                onRandomBytePoolGenerated(bytePool);
            });
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random 16bit ints.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomIntPoolGenerated"/>.
        /// </summary>
        /// <param name="count">The amount of 16bit ints generated</param>
        /// <param name="onRandomIntPoolGenerated">The callback called when the pool is available</param>
        public void GenerateInt16Pool(int count, Action<List<int>> onRandomIntPoolGenerated) {
            GenerateIntNbitsPool(16, count, onRandomIntPoolGenerated);
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random ints.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomIntPoolGenerated"/>.
        /// </summary>
        /// <param name="count">The amount of ints generated</param>
        /// <param name="onRandomIntPoolGenerated">The callback called when the pool is available</param>
        public void GenerateInt32Pool(int count, Action<List<int>> onRandomIntPoolGenerated) {
            GenerateIntNbitsPool(32, count, onRandomIntPoolGenerated);
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random floats in the range [0, 1].
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomFloatPoolGenerated"/>.
        /// </summary>
        /// <param name="count">The amount of floats generated</param>
        /// <param name="onRandomFloatPoolGenerated">The callback called when the pool is available</param>
        public void GenerateFloatPool(int count, Action<List<float>> onRandomFloatPoolGenerated) {
            GenerateIntNbitsPool(32, count, (intPool) => {
                List<float> floatPool = new List<float>();
                foreach (int i in intPool) {
                    floatPool.Add(Int32ToFloat(i));
                }
                onRandomFloatPoolGenerated(floatPool);
            });
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random floats in the range [<paramref name="min"/>, <paramref name="max"/>].
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomFloatPoolGenerated"/>.
        /// </summary>
        /// <param name="count">The amount of floats generated</param>
        /// <param name="min">The smallest number generated</param>
        /// <param name="max">The largest number generated</param>
        /// <param name="onRandomFloatPoolGenerated">The callback called when the pool is available</param>
        public void GenerateFloatPoolInRange(int count, float min, float max, Action<List<float>> onRandomFloatPoolGenerated) {
            GenerateIntNbitsPool(32, count, (intPool) => {
                List<float> floatPool = new List<float>();
                foreach (int i in intPool) {
                    floatPool.Add(Int32ToFloat(i, min, max));
                }
                onRandomFloatPoolGenerated(floatPool);
            });
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random ints of n <paramref name="bits"/>.
        /// It makes an asynchronous operation so the value is returned through 
        /// the callback <see cref="OnRandomFloatPoolGenerated"/>.
        /// </summary>
        /// <param name="bits">The number of bits used to generate the int</param>
        /// <param name="count">The amount of ints generated</param>
        /// <param name="onRandomIntPoolGenerated">The callback called when the pool is available</param>
        public void GenerateIntNbitsPool(int bits, int count, Action<List<int>> onRandomIntPoolGenerated) {
            executionSession.RequestBackendConfig((backendConfig) => {
                int codeRegs = Mathf.Min(backendConfig.qubitsCount, bits);
                int shotsNeededPerItem = Mathf.CeilToInt((float)bits / codeRegs);
                QASMExecutable qasmExe = new QASMExecutable(RandomNRegisterCode(codeRegs), shotsNeededPerItem * count);

                executionSession.ExecuteCodeRawResult(qasmExe, (response) => {
                    List<int> pool = new List<int>();
                    for (int i = 0; i < count; i++) {
                        int rng = 0;
                        int padding = i * shotsNeededPerItem;
                        for (int j = 0; j < shotsNeededPerItem; j++) {
                            rng += response.rawResult[j + padding] << (j * codeRegs);
                        }
                        rng = ClampToBits(rng, bits);
                        pool.Add(rng);
                    }
                    onRandomIntPoolGenerated(pool);
                });
            });
        }

        #endregion


        #region Pool Of Values Generation Methods

        /// <summary>
        /// Generates an auto-refill pool of <paramref name="capacity"/> true random booleans.
        /// It makes an asynchronus refill considering the <paramref name="refillPolicy"/>.
        /// It take a few time to be refilled. See <see cref="InfiniteQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">The amount of values holded.</param>
        /// <param name="refillPolicy">The rules to perform an asynchronus refill.</param>
        /// <returns>A configured <see cref="InfiniteQueue{T}"/></returns>
        public InfiniteQueue<bool> InfiniteBoolPool(int capacity, RefillPolicy refillPolicy) {
            return new InfiniteQueue<bool>(capacity, GenerateBoolPool, refillPolicy);
        }

        /// <summary>
        /// Generates an auto-refill pool of <paramref name="capacity"/> true random bytes.
        /// It makes an asynchronus refill considering the <paramref name="refillPolicy"/>.
        /// It take a few time to be refilled. See <see cref="InfiniteQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">The amount of values holded.</param>
        /// <param name="refillPolicy">The rules to perform an asynchronus refill.</param>
        /// <returns>A configured <see cref="InfiniteQueue{T}"/></returns>
        public InfiniteQueue<byte> InfiniteBytePool(int capacity, RefillPolicy refillPolicy) {
            return new InfiniteQueue<byte>(capacity, GenerateBytePool, refillPolicy);
        }

        /// <summary>
        /// Generates an auto-refill pool of <paramref name="capacity"/> true random 16bit ints.
        /// It makes an asynchronus refill considering the <paramref name="refillPolicy"/>.
        /// It take a few time to be refilled. See <see cref="InfiniteQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">The amount of values holded.</param>
        /// <param name="refillPolicy">The rules to perform an asynchronus refill.</param>
        /// <returns>A configured <see cref="InfiniteQueue{T}"/></returns>
        public InfiniteQueue<int> InfiniteInt16Pool(int capacity, RefillPolicy refillPolicy) {
            return new InfiniteQueue<int>(capacity, GenerateInt16Pool, refillPolicy);
        }

        /// <summary>
        /// Generates an auto-refill pool of <paramref name="capacity"/> true random ints.
        /// It makes an asynchronus refill considering the <paramref name="refillPolicy"/>.
        /// It take a few time to be refilled. See <see cref="InfiniteQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">The amount of values holded.</param>
        /// <param name="refillPolicy">The rules to perform an asynchronus refill.</param>
        /// <returns>A configured <see cref="InfiniteQueue{T}"/></returns>
        public InfiniteQueue<int> InfiniteInt32Pool(int capacity, RefillPolicy refillPolicy) {
            return new InfiniteQueue<int>(capacity, GenerateInt32Pool, refillPolicy);
        }

        /// <summary>
        /// Generates an auto-refill pool of <paramref name="capacity"/> true random floats in the range [0, 1].
        /// It makes an asynchronus refill considering the <paramref name="refillPolicy"/>.
        /// It take a few time to be refilled. See <see cref="InfiniteQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">The amount of values holded.</param>
        /// <param name="refillPolicy">The rules to perform an asynchronus refill.</param>
        /// <returns>A configured <see cref="InfiniteQueue{T}"/></returns>
        public InfiniteQueue<float> InfiniteFloatPool(int capacity, RefillPolicy refillPolicy) {
            return new InfiniteQueue<float>(capacity, GenerateFloatPool, refillPolicy);
        }

        /// <summary>
        /// Generates a pool of <paramref name="count"/> true random floats in the range [<paramref name="min"/>, <paramref name="max"/>].
        /// Generates an auto-refill pool of <paramref name="capacity"/> true random floats in the range [<paramref name="min"/>, <paramref name="max"/>].
        /// It makes an asynchronus refill considering the <paramref name="refillPolicy"/>.
        /// It take a few time to be refilled. See <see cref="InfiniteQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">The amount of values holded.</param>
        /// <param name="min">The smallest number generated</param>
        /// <param name="max">The largest number generated</param>
        /// <param name="refillPolicy">The rules to perform an asynchronus refill.</param>
        /// <returns>A configured <see cref="InfiniteQueue{T}"/></returns>
        public InfiniteQueue<float> InfiniteFloatPoolInRange(int capacity, float min, float max, RefillPolicy refillPolicy) {
            return new InfiniteQueue<float>(capacity, (count, pool) => GenerateFloatPoolInRange(count, min, max, pool), refillPolicy);
        }

        #endregion

        /// <summary>
        /// Limits <paramref name="n"/> to <paramref name="bits"/>
        /// </summary>
        /// <param name="n">The number to clamp</param>
        /// <param name="bits">Max number of bits</param>
        /// <returns>The clamped number</returns>
        private int ClampToBits(int n, int bits) {
            if (bits < 32) {
                int mask = (1 << bits) - 1;
                return n & mask;
            }
            return n;
        }

        /// <summary>
        /// Create qasm code for generate <paramref name="n"/> random bits using registers.
        /// </summary>
        /// <param name="n">The number of registers</param>
        /// <returns>The qasm code</returns>
        private string RandomNRegisterCode(int n) {
            // Header
            string qasmCode = "include \"qelib1.inc\";";

            // Registers
            qasmCode += $"qreg q[{n}]; creg c[{n}];";

            // Circuit
            for (int i = 0; i < n; i++) {
                qasmCode += $"h q[{i}];";
            }
            for (int i = 0; i < n; i++) {
                qasmCode += $"measure q[{i}] -> c[{i}];";
            }

            return qasmCode;
        }

        private float Int32ToFloat(int i) {
            return Mathf.Abs((float)i / int.MaxValue);
        }

        private float Int32ToFloat(int i, float min, float max) {
            return Int32ToFloat(i) * (max - min) + min;
        }

#if UNITY_EDITOR
        #region Test methods
        [ContextMenu("Generate Bool")]
        private void TryGenerateBool() {
            GenerateBool((b) => Debug.Log($"Generated bool: {b}"));
        }
        [ContextMenu("Generate 100 Bool")]
        private void TryGenerateLotsOfBool() {
            GenerateBoolPool(100, (pool) => {
                string s = "[ ";
                foreach (bool i in pool) {
                    s += $"{i}, ";
                }
                s += "]";
                Debug.Log($"Generated: {s}");
            });
        }
        [ContextMenu("Generate Byte")]
        private void TryGenerateByte() {
            GenerateByte((b) => Debug.Log($"Generated byte: {b}"));
        }
        [ContextMenu("Generate 100 Byte")]
        private void TryGenerateLotsOfByte() {
            GenerateBytePool(100, (pool) => {
                string s = "[ ";
                foreach (byte i in pool) {
                    s += $"{i}, ";
                }
                s += "]";
                Debug.Log($"Generated: {s}");
            });
        }
        [ContextMenu("Generate Int16")]
        private void TryGenerateInt16() {
            GenerateInt16((b) => Debug.Log($"Generated int16: {b}"));
        }
        [ContextMenu("Generate 100 Int16")]
        private void TryGenerateLotsOfInt16() {
            GenerateInt16Pool(100, (pool) => {
                string s = "[ ";
                foreach (int i in pool) {
                    s += $"{i}, ";
                }
                s += "]";
                Debug.Log($"Generated: {s}");
            });
        }
        [ContextMenu("Generate Int32")]
        private void TryGenerateInt32() {
            GenerateInt32((b) => Debug.Log($"Generated int32: {b}"));
        }
        [ContextMenu("Generate 100 Int32")]
        private void TryGenerateLotsOfInt32() {
            GenerateInt32Pool(100, (pool) => {
                string s = "[ ";
                foreach (int i in pool) {
                    s += $"{i}, ";
                }
                s += "]";
                Debug.Log($"Generated: {s}");
            });
        }
        [ContextMenu("Generate Float")]
        private void TryGenerateFloat() {
            GenerateFloat((b) => Debug.Log($"Generated float: {b}"));
        }
        [ContextMenu("Generate Float Range [3, 10]")]
        private void TryGenerateFloatRange() {
            GenerateFloatInRange(3f, 10f, (b) => Debug.Log($"Generated float: {b}"));
        }
        [ContextMenu("Generate 100 float")]
        private void TryGenerateLotsOfFloat() {
            GenerateFloatPool(100, (pool) => {
                string s = "[ ";
                foreach (float i in pool) {
                    s += $"{i}, ";
                }
                s += "]";
                Debug.Log($"Generated: {s}");
            });
        }
        [ContextMenu("Generate 100 float [-4, 4]")]
        private void TryGenerateLotsOfFloatRange() {
            GenerateFloatPoolInRange(100, -4f, 4f, (pool) => {
                string s = "[ ";
                foreach (float i in pool) {
                    s += $"{i}, ";
                }
                s += "]";
                Debug.Log($"Generated: {s}");
            });
        }

        [ContextMenu("Generate inf Bools")]
        private void TryGetInfiniteBool() {
            InfiniteQueue<bool> queue = InfiniteBoolPool(10, RefillPolicy.KEEP_FULL);
            StartCoroutine(ExtractFromQueue(queue, 30));
        }
        [ContextMenu("Generate inf Floats")]
        private void TryGetInfiniteFloatsRange() {
            InfiniteQueue<float> queue = InfiniteFloatPoolInRange(20, -10, 100, RefillPolicy.EMPTY);
            StartCoroutine(ExtractFromQueue(queue, 30));
        }
        [ContextMenu("Generate inf Ints")]
        private void TryGetInfiniteIntsRange() {
            InfiniteQueue<int> queue = InfiniteInt16Pool(10, RefillPolicy.HALF_QUEUE);
            StartCoroutine(ExtractFromQueue(queue, 15));
        }
        private IEnumerator ExtractFromQueue<T>(InfiniteQueue<T> queue, int extractions) {
            for (int i = 0; i < extractions; i++) {
                yield return new WaitWhile(() => queue.isEmpty);
                Debug.Log(queue.PopNext());
            }
            Debug.Log($"Count : {queue.count}");
        }

        #endregion
#endif

    }
}