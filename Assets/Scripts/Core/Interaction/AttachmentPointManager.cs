using UnityEngine;
using System.Collections.Generic;
using System;

namespace KeyboardSim.Core.Interaction
{
    /// <summary>
    /// Manages a collection of attachment points for a component.
    /// </summary>
    public class AttachmentPointManager : MonoBehaviour
    {
        [Tooltip("Automatically find all attachment points in children")]
        [SerializeField] private bool _autoFindAttachmentPoints = true;
        
        [Tooltip("The attachment points managed by this component")]
        [SerializeField] private List<AttachmentPoint> _attachmentPoints = new List<AttachmentPoint>();
        
        // State tracking
        private Dictionary<GameObject, List<AttachmentPoint>> _validPointsCache = new Dictionary<GameObject, List<AttachmentPoint>>();
        private List<AttachmentPoint> _highlightedPoints = new List<AttachmentPoint>();
        
        // Events
        public event Action<GameObject, AttachmentPoint> OnObjectAttached;
        public event Action<GameObject, AttachmentPoint> OnObjectDetached;
        
        private void Awake()
        {
            if (_autoFindAttachmentPoints)
            {
                FindAttachmentPoints();
            }
            
            // Subscribe to attachment point events
            SubscribeToAttachmentPointEvents();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from attachment point events
            UnsubscribeFromAttachmentPointEvents();
        }
        
        private void FindAttachmentPoints()
        {
            // Clear existing list
            _attachmentPoints.Clear();
            
            // Find all attachment points in children
            AttachmentPoint[] points = GetComponentsInChildren<AttachmentPoint>();
            _attachmentPoints.AddRange(points);
        }
        
        private void SubscribeToAttachmentPointEvents()
        {
            foreach (AttachmentPoint point in _attachmentPoints)
            {
                point.OnObjectAttached += (obj) => OnObjectAttached?.Invoke(obj, point);
                point.OnObjectDetached += (obj) => OnObjectDetached?.Invoke(obj, point);
            }
        }
        
        private void UnsubscribeFromAttachmentPointEvents()
        {
            foreach (AttachmentPoint point in _attachmentPoints)
            {
                // Since we're using lambda expressions, we can't directly unsubscribe
                // This is a limitation of the current implementation
                // In a more robust implementation, we'd store the delegate references
            }
        }
        
        /// <summary>
        /// Highlight all valid attachment points for the given object
        /// </summary>
        public void HighlightAvailablePoints(GameObject obj, bool highlight)
        {
            // Clear previously highlighted points
            ClearHighlights();
            
            // If no object or not highlighting, just return after clearing
            if (obj == null || !highlight) return;
            
            // Find valid points for this object
            List<AttachmentPoint> validPoints = GetValidAttachmentPoints(obj);
            
            // Highlight valid points
            foreach (AttachmentPoint point in validPoints)
            {
                point.ShowVisualizer(true, true);
                _highlightedPoints.Add(point);
            }
        }
        
        private void ClearHighlights()
        {
            foreach (AttachmentPoint point in _highlightedPoints)
            {
                point.ShowVisualizer(false);
            }
            
            _highlightedPoints.Clear();
        }
        
        /// <summary>
        /// Get all valid attachment points for the given object
        /// </summary>
        public List<AttachmentPoint> GetValidAttachmentPoints(GameObject obj)
        {
            // Check cache first
            if (_validPointsCache.ContainsKey(obj))
            {
                return _validPointsCache[obj];
            }
            
            // Find valid points
            List<AttachmentPoint> validPoints = new List<AttachmentPoint>();
            
            foreach (AttachmentPoint point in _attachmentPoints)
            {
                if (point.CanAttach(obj))
                {
                    validPoints.Add(point);
                }
            }
            
            // Cache results
            _validPointsCache[obj] = validPoints;
            
            return validPoints;
        }
        
        /// <summary>
        /// Find the nearest valid attachment point to the given position
        /// </summary>
        public AttachmentPoint GetNearestValidPoint(GameObject obj, Vector3 position)
        {
            List<AttachmentPoint> validPoints = GetValidAttachmentPoints(obj);
            
            if (validPoints.Count == 0) return null;
            
            AttachmentPoint nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (AttachmentPoint point in validPoints)
            {
                float distance = Vector3.Distance(point.transform.position, position);
                
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = point;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Attach an object to the nearest valid attachment point
        /// </summary>
        public bool AttachToNearestPoint(GameObject obj, Vector3 position)
        {
            AttachmentPoint nearest = GetNearestValidPoint(obj, position);
            
            if (nearest == null) return false;
            
            return nearest.AttachObject(obj);
        }
        
        /// <summary>
        /// Check if the manager has any valid points for the given object
        /// </summary>
        public bool HasValidAttachmentPoints(GameObject obj)
        {
            return GetValidAttachmentPoints(obj).Count > 0;
        }
        
        /// <summary>
        /// Reset the cache when objects change
        /// </summary>
        public void ClearCache()
        {
            _validPointsCache.Clear();
        }
        
        /// <summary>
        /// Get all current attachment points
        /// </summary>
        public List<AttachmentPoint> GetAttachmentPoints()
        {
            return _attachmentPoints;
        }
        
        /// <summary>
        /// Get all currently occupied attachment points
        /// </summary>
        public List<AttachmentPoint> GetOccupiedPoints()
        {
            List<AttachmentPoint> occupiedPoints = new List<AttachmentPoint>();
            
            foreach (AttachmentPoint point in _attachmentPoints)
            {
                if (point.IsOccupied())
                {
                    occupiedPoints.Add(point);
                }
            }
            
            return occupiedPoints;
        }
    }
} 