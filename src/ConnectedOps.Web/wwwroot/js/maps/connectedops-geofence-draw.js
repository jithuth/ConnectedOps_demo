/**
 * ConnectedOps Geofence Drawing & Editing Helper for MapLibre GL JS
 * Enables interactive creation and editing of Circle and Polygon geofences.
 */
class ConnectedOpsGeofenceDraw {
    constructor(mapInstance, options = {}) {
        this.coMap = mapInstance;
        this.map = mapInstance.map;
        this.options = Object.assign({
            onShapeChange: null,
            primaryColor: '#3b82f6',
            fillOpacity: 0.25
        }, options);

        this.currentMode = 'none'; // 'none' | 'circle' | 'polygon' | 'edit'
        this.circleData = null; // { center: [lng, lat], radiusMeters: 500 }
        this.polygonCoordinates = []; // [[lng, lat], ...]
        this.tempMarkers = [];

        this._boundOnClick = this._handleClick.bind(this);
        this._boundOnMouseMove = this._handleMouseMove.bind(this);
        this._boundOnDblClick = this._handleDblClick.bind(this);

        this._initSources();
    }

    _initSources() {
        if (!this.map.getSource('geofence-draw-source')) {
            this.map.addSource('geofence-draw-source', {
                type: 'geojson',
                data: {
                    type: 'FeatureCollection',
                    features: []
                }
            });

            this.map.addLayer({
                id: 'geofence-draw-fill',
                type: 'fill',
                source: 'geofence-draw-source',
                paint: {
                    'fill-color': this.options.primaryColor,
                    'fill-opacity': this.options.fillOpacity
                }
            });

            this.map.addLayer({
                id: 'geofence-draw-line',
                type: 'line',
                source: 'geofence-draw-source',
                paint: {
                    'line-color': this.options.primaryColor,
                    'line-width': 3,
                    'line-dasharray': [2, 2]
                }
            });

            this.map.addLayer({
                id: 'geofence-draw-points',
                type: 'circle',
                source: 'geofence-draw-source',
                filter: ['==', '$type', 'Point'],
                paint: {
                    'circle-radius': 6,
                    'circle-color': '#ffffff',
                    'circle-stroke-color': this.options.primaryColor,
                    'circle-stroke-width': 2
                }
            });
        }
    }

    startCircle(initialCenter = null, initialRadius = 500) {
        this.stop();
        this.currentMode = 'circle';
        this.map.getCanvas().style.cursor = 'crosshair';

        if (initialCenter) {
            this.circleData = {
                center: [initialCenter.lng, initialCenter.lat],
                radiusMeters: initialRadius
            };
            this._renderCircle();
            this._notifyChange();
        } else {
            this.circleData = null;
        }

        this.map.on('click', this._boundOnClick);
    }

    startPolygon(initialCoordinates = null) {
        this.stop();
        this.currentMode = 'polygon';
        this.map.getCanvas().style.cursor = 'crosshair';
        this.polygonCoordinates = initialCoordinates ? [...initialCoordinates] : [];

        if (this.polygonCoordinates.length > 0) {
            this._renderPolygon();
            this._notifyChange();
        }

        this.map.on('click', this._boundOnClick);
        this.map.on('dblclick', this._boundOnDblClick);
    }

    setCircleRadius(radiusMeters) {
        if (this.circleData && this.circleData.center) {
            this.circleData.radiusMeters = Math.max(10, radiusMeters);
            this._renderCircle();
            this._notifyChange();
        }
    }

    stop() {
        this.currentMode = 'none';
        this.map.getCanvas().style.cursor = '';
        this.map.off('click', this._boundOnClick);
        this.map.off('mousemove', this._boundOnMouseMove);
        this.map.off('dblclick', this._boundOnDblClick);
        this._clearTempMarkers();
    }

    clear() {
        this.stop();
        this.circleData = null;
        this.polygonCoordinates = [];
        const source = this.map.getSource('geofence-draw-source');
        if (source) {
            source.setData({ type: 'FeatureCollection', features: [] });
        }
    }

    undoLastPoint() {
        if (this.currentMode === 'polygon' && this.polygonCoordinates.length > 0) {
            this.polygonCoordinates.pop();
            this._renderPolygon();
            this._notifyChange();
        }
    }

    getShapeData() {
        if (this.currentMode === 'circle' || this.circleData) {
            if (!this.circleData || !this.circleData.center) return null;
            return {
                type: 'Circle',
                latitude: this.circleData.center[1],
                longitude: this.circleData.center[0],
                radiusMeters: this.circleData.radiusMeters
            };
        }

        if (this.currentMode === 'polygon' || this.polygonCoordinates.length >= 3) {
            if (this.polygonCoordinates.length < 3) return null;

            // Ensure closed loop
            const coords = [...this.polygonCoordinates];
            const first = coords[0];
            const last = coords[coords.length - 1];
            if (first[0] !== last[0] || first[1] !== last[1]) {
                coords.push([first[0], first[1]]);
            }

            // Calculate centroid
            let sumLat = 0, sumLng = 0;
            for (let i = 0; i < coords.length - 1; i++) {
                sumLng += coords[i][0];
                sumLat += coords[i][1];
            }
            const count = coords.length - 1;

            const geoJson = {
                type: 'Polygon',
                coordinates: [coords]
            };

            return {
                type: 'Polygon',
                latitude: sumLat / count,
                longitude: sumLng / count,
                polygonGeoJson: JSON.stringify(geoJson),
                coordinates: coords
            };
        }

        return null;
    }

    _handleClick(e) {
        const lng = e.lngLat.lng;
        const lat = e.lngLat.lat;

        if (this.currentMode === 'circle') {
            if (!this.circleData) {
                this.circleData = {
                    center: [lng, lat],
                    radiusMeters: 500
                };
                this._renderCircle();
                this._notifyChange();
            } else {
                // Second click sets radius based on distance from center
                const distanceMeters = this._haversineDistance(this.circleData.center, [lng, lat]);
                this.circleData.radiusMeters = Math.max(20, Math.round(distanceMeters));
                this._renderCircle();
                this._notifyChange();
            }
        } else if (this.currentMode === 'polygon') {
            this.polygonCoordinates.push([lng, lat]);
            this._renderPolygon();
            this._notifyChange();
        }
    }

    _handleDblClick(e) {
        if (this.currentMode === 'polygon' && this.polygonCoordinates.length >= 3) {
            e.preventDefault();
            this.stop();
            this._renderPolygon();
            this._notifyChange();
        }
    }

    _handleMouseMove(e) {
        // Optional live preview when moving mouse
    }

    _renderCircle() {
        if (!this.circleData || !this.circleData.center) return;

        const center = this.circleData.center;
        const radius = this.circleData.radiusMeters;
        const points = 64;
        const coords = [];
        const km = radius / 1000;
        const distanceX = km / (111.320 * Math.cos(center[1] * Math.PI / 180));
        const distanceY = km / 110.574;

        for (let i = 0; i < points; i++) {
            const theta = (i / points) * (2 * Math.PI);
            const x = distanceX * Math.cos(theta);
            const y = distanceY * Math.sin(theta);
            coords.push([center[0] + x, center[1] + y]);
        }
        coords.push(coords[0]); // close polygon

        const polygonFeature = {
            type: 'Feature',
            geometry: {
                type: 'Polygon',
                coordinates: [coords]
            }
        };

        const centerFeature = {
            type: 'Feature',
            geometry: {
                type: 'Point',
                coordinates: center
            }
        };

        const source = this.map.getSource('geofence-draw-source');
        if (source) {
            source.setData({
                type: 'FeatureCollection',
                features: [polygonFeature, centerFeature]
            });
        }
    }

    _renderPolygon() {
        if (this.polygonCoordinates.length === 0) return;

        const features = [];
        // Point features for vertices
        this.polygonCoordinates.forEach((coord, idx) => {
            features.push({
                type: 'Feature',
                properties: { index: idx },
                geometry: {
                    type: 'Point',
                    coordinates: coord
                }
            });
        });

        if (this.polygonCoordinates.length >= 2) {
            const coords = [...this.polygonCoordinates];
            if (this.polygonCoordinates.length >= 3) {
                coords.push(coords[0]); // closed preview
                features.push({
                    type: 'Feature',
                    geometry: {
                        type: 'Polygon',
                        coordinates: [coords]
                    }
                });
            } else {
                features.push({
                    type: 'Feature',
                    geometry: {
                        type: 'LineString',
                        coordinates: coords
                    }
                });
            }
        }

        const source = this.map.getSource('geofence-draw-source');
        if (source) {
            source.setData({
                type: 'FeatureCollection',
                features: features
            });
        }
    }

    _clearTempMarkers() {
        this.tempMarkers.forEach(m => m.remove());
        this.tempMarkers = [];
    }

    _haversineDistance(coord1, coord2) {
        const R = 6371000; // meters
        const lat1 = coord1[1] * Math.PI / 180;
        const lat2 = coord2[1] * Math.PI / 180;
        const dLat = (coord2[1] - coord1[1]) * Math.PI / 180;
        const dLon = (coord2[0] - coord1[0]) * Math.PI / 180;

        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1) * Math.cos(lat2) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return R * c;
    }

    _notifyChange() {
        if (typeof this.options.onShapeChange === 'function') {
            this.options.onShapeChange(this.getShapeData());
        }
    }
}

window.ConnectedOpsGeofenceDraw = ConnectedOpsGeofenceDraw;
