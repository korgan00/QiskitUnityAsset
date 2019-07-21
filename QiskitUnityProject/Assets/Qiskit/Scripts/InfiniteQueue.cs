using System;
using System.Collections.Generic;

public partial class InfiniteQueue<T> {

    public delegate void ElementsProvider(int count, Action<List<T>> pool);

    private readonly ElementsProvider _provider;

    private readonly List<T> _generatedElements;
    private readonly Queue<Action> _elementsRequests;

    private readonly RefillPolicy _policy;
    /// <summary>
    /// The initial capacity of the queue.
    /// It is the value considered for checking if queue must be refilled.
    /// </summary>
    public int capacity { get; }

    /// <summary>
    /// Current number of loaded elements
    /// </summary>
    public int count => _generatedElements.Count;

    /// <summary>
    /// true if there are no loaded elements.
    /// </summary>
    public bool isEmpty => count == 0;

    private bool _requesting;

    public InfiniteQueue(int capacity, ElementsProvider elementsProviderFunction, RefillPolicy policy = RefillPolicy.HALF_QUEUE) {
        _generatedElements = new List<T>();
        _elementsRequests = new Queue<Action>();
        _requesting = false;
        _policy = policy;
        this.capacity = capacity;
        _provider = elementsProviderFunction;
        CheckListState();
    }

    /// <summary>
    /// Removes and returns a element.
    /// </summary>
    /// <returns>The element</returns>
    public T PopNext() {

        T t = _generatedElements[count - 1];
        _generatedElements.RemoveAt(count - 1);
        CheckListState();

        return t;
    }

    /// <summary>
    /// Returns a element.
    /// </summary>
    /// <returns>The element</returns>
    public T PeekNext() {
        T t = _generatedElements[count - 1];
        CheckListState();

        return t;
    }

    /// <summary>
    /// Removes and returns a element.
    /// </summary>
    /// <returns>The element</returns>
    public void PopNext(Action<T> onElementGenerated) {
        if (isEmpty) {
            _elementsRequests.Enqueue(() => onElementGenerated(PopNext()));
        } else {
            onElementGenerated(PopNext());
        }

        CheckListState();
    }

    /// <summary>
    /// Returns a element.
    /// </summary>
    /// <returns>The element</returns>
    public void PeekNext(Action<T> onElementGenerated) {
        if (isEmpty) {
            _elementsRequests.Enqueue(() => onElementGenerated(PeekNext()));
        } else {
            onElementGenerated(PeekNext());
        }

        CheckListState();
    }

    private void CheckListState() {
        if (!_requesting && MustRefill()) {
            _requesting = true;

            _provider(RefillNeed(), (pool) => {
                _generatedElements.AddRange(pool);
                _requesting = false;
                CheckListState();
            });
        }
    }

    private int RefillNeed() {
        int need = capacity - count;
        return _policy == RefillPolicy.KEEP_FULL ? (int)(need + (capacity * 0.5f)) : need;
    }

    private bool MustRefill() {
        switch (_policy) {
            case RefillPolicy.EMPTY: return isEmpty;
            case RefillPolicy.HALF_QUEUE: return count <= (capacity / 2);
            case RefillPolicy.KEEP_FULL: return count < capacity;
        }
        return false;
    }

}