/**
 * ConnectedOps MapLibre GL JS Wrapper
 * Provides high-performance live fleet mapping, clustering, vehicle rotation,
 * geofence rendering, and historical trail visualization.
 */
class ConnectedOpsMap {
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.options = Object.assign({
            styleUrl: 'https://tiles.openfreemap.org/styles/bright',
            defaultCenter: [55.2708, 25.2048], // [lon, lat]
            defaultZoom: 12,
            minZoom: 2,
            maxZoom: 19,
            enableClustering: true,
            onVehicleSelect: null
        }, options);

        this.map = null;
        this.isLoaded = false;
        this.selectedVehicleId = null;
        this.popup = null;
        this.init();
    }

    init() {
        if (!window.maplibregl) {
            console.error('MapLibre GL JS is not loaded.');
            return;
        }

        this.map = new maplibregl.Map({
            container: this.containerId,
            style: this.options.styleUrl,
            center: this.options.defaultCenter,
            zoom: this.options.defaultZoom,
            minZoom: this.options.minZoom,
            maxZoom: this.options.maxZoom
        });

        // Add standard navigation controls
        this.map.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), 'top-right');
        this.map.addControl(new maplibregl.FullscreenControl(), 'top-right');

        this.map.on('load', () => {
            this.isLoaded = true;
            this._setupLayers();
            if (this.options.onReady) {
                this.options.onReady(this);
            }
        });
    }

    _setupLayers() {
        // 1. Vehicle GeoJSON Source with Clustering
        this.map.addSource('vehicles-source', {
            type: 'geojson',
            data: { type: 'FeatureCollection', features: [] },
            cluster: this.options.enableClustering,
            clusterMaxZoom: 14,
            clusterRadius: 50
        });

        // Cluster Circles Layer
        this.map.addLayer({
            id: 'clusters',
            type: 'circle',
            source: 'vehicles-source',
            filter: ['has', 'point_count'],
            paint: {
                'circle-color': [
                    'step',
                    ['get', 'point_count'],
                    '#3b82f6', // blue < 10
                    10, '#f59e0b', // amber 10-50
                    50, '#10b981'  // green >= 50
                ],
                'circle-radius': [
                    'step',
                    ['get', 'point_count'],
                    18,
                    10, 24,
                    50, 30
                ],
                'circle-stroke-width': 3,
                'circle-stroke-color': '#ffffff',
                'circle-opacity': 0.88
            }
        });

        // Cluster Count Text Layer
        this.map.addLayer({
            id: 'cluster-count',
            type: 'symbol',
            source: 'vehicles-source',
            filter: ['has', 'point_count'],
            layout: {
                'text-field': '{point_count_abbreviated}',
                'text-font': ['Open Sans Bold'],
                'text-size': 13
            },
            paint: {
                'text-color': '#ffffff'
            }
        });

        // Individual Vehicle Marker Outer Circle Layer (Marker State Colors)
        this.map.addLayer({
            id: 'unclustered-vehicles',
            type: 'circle',
            source: 'vehicles-source',
            filter: ['!', ['has', 'point_count']],
            paint: {
                'circle-color': [
                    'match',
                    ['get', 'markerState'],
                    'Moving', '#10b981',   // Emerald Green
                    'Stopped', '#f59e0b',  // Amber/Orange
                    'Parked', '#3b82f6',   // Blue
                    'Offline', '#ef4444',  // Red
                    /* default / Untracked */ '#6b7280'
                ],
                'circle-radius': 9,
                'circle-stroke-width': 2.5,
                'circle-stroke-color': '#ffffff',
                'circle-opacity': 0.95
            }
        });

        // Vehicle Labels (Vehicle Number + Speed)
        this.map.addLayer({
            id: 'vehicle-labels',
            type: 'symbol',
            source: 'vehicles-source',
            filter: ['!', ['has', 'point_count']],
            layout: {
                'text-field': ['concat', ['get', 'vehicleNumber'], ' (', ['to-string', ['get', 'speedKph']], ' km/h)'],
                'text-font': ['Open Sans Semibold'],
                'text-size': 11,
                'text-offset': [0, 1.4],
                'text-anchor': 'top',
                'text-optional': true
            },
            paint: {
                'text-color': '#1e293b',
                'text-halo-color': '#ffffff',
                'text-halo-width': 1.5
            }
        });

        // Click handler on clusters -> zoom in
        this.map.on('click', 'clusters', (e) => {
            const features = this.map.queryRenderedFeatures(e.point, { layers: ['clusters'] });
            const clusterId = features[0].properties.cluster_id;
            this.map.getSource('vehicles-source').getClusterExpansionZoom(clusterId, (err, zoom) => {
                if (err) return;
                this.map.easeTo({
                    center: features[0].geometry.coordinates,
                    zoom: zoom
                });
            });
        });

        // Click handler on individual vehicle
        this.map.on('click', 'unclustered-vehicles', (e) => {
            if (!e.features.length) return;
            const props = e.features[0].properties;
            const coords = e.features[0].geometry.coordinates.slice();

            this._handleVehicleClick(props, coords);
        });

        // Hover cursor pointer
        this.map.on('mouseenter', 'clusters', () => { this.map.getCanvas().style.cursor = 'pointer'; });
        this.map.on('mouseleave', 'clusters', () => { this.map.getCanvas().style.cursor = ''; });
        this.map.on('mouseenter', 'unclustered-vehicles', () => { this.map.getCanvas().style.cursor = 'pointer'; });
        this.map.on('mouseleave', 'unclustered-vehicles', () => { this.map.getCanvas().style.cursor = ''; });
    }

    _handleVehicleClick(props, coords) {
        this.selectedVehicleId = props.vehicleId;

        // Display brief popup
        if (this.popup) {
            this.popup.remove();
        }

        const stateBadgeClass =
            props.markerState === 'Moving' ? 'bg-success' :
            props.markerState === 'Stopped' ? 'bg-warning text-dark' :
            props.markerState === 'Parked' ? 'bg-primary' :
            props.markerState === 'Offline' ? 'bg-danger' : 'bg-secondary';

        const html = `
            <div class="p-1" style="min-width: 180px;">
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <strong class="text-primary">${props.vehicleNumber}</strong>
                    <span class="badge ${stateBadgeClass}">${props.markerState}</span>
                </div>
                <div class="small text-muted mb-1">${props.displayName || ''}</div>
                <div class="small"><strong>Driver:</strong> ${props.driverName || 'Unassigned'}</div>
                <div class="small"><strong>Speed:</strong> ${props.speedKph || 0} km/h</div>
                <div class="small"><strong>Ignition:</strong> ${props.ignition === 'true' || props.ignition === true ? '<span class="text-success fw-bold">ON</span>' : '<span class="text-muted">OFF</span>'}</div>
                <div class="small"><strong>Branch:</strong> ${props.branchName || '—'}</div>
                <hr class="my-1" />
                <div class="d-flex justify-content-between">
                    <a href="/Vehicles/Details?id=${props.vehicleId}" class="btn btn-xs btn-outline-primary py-0 px-1" target="_blank">Details</a>
                    <a href="/Maps/History?vehicleId=${props.vehicleId}" class="btn btn-xs btn-outline-secondary py-0 px-1" target="_blank">Trail</a>
                </div>
            </div>
        `;

        this.popup = new maplibregl.Popup({ offset: 12 })
            .setLngLat(coords)
            .setHTML(html)
            .addTo(this.map);

        // Trigger callback for parent UI (e.g. vehicle details side panel)
        if (typeof this.options.onVehicleSelect === 'function') {
            this.options.onVehicleSelect(props);
        }
    }

    /**
     * Updates live vehicle data GeoJSON source smoothly.
     */
    setVehiclesGeoJson(geoJson) {
        if (!this.map || !this.isLoaded) return;
        const source = this.map.getSource('vehicles-source');
        if (source) {
            source.setData(geoJson);
        }
    }

    /**
     * Highlights and centers a vehicle on the map.
     */
    focusVehicle(lng, lat, zoom = 15) {
        if (!this.map) return;
        this.map.flyTo({
            center: [lng, lat],
            zoom: zoom,
            speed: 1.4,
            curve: 1.2
        });
    }

    /**
     * Renders geofences (Circle and Polygon) on the map.
     */
    renderGeofences(geofencesList) {
        if (!this.map || !this.isLoaded) return;

        // Clean up previous geofence source/layers if exist
        if (this.map.getLayer('geofence-polygons-fill')) this.map.removeLayer('geofence-polygons-fill');
        if (this.map.getLayer('geofence-polygons-line')) this.map.removeLayer('geofence-polygons-line');
        if (this.map.getLayer('geofence-circles-fill')) this.map.removeLayer('geofence-circles-fill');
        if (this.map.getLayer('geofence-circles-line')) this.map.removeLayer('geofence-circles-line');
        if (this.map.getLayer('geofence-labels')) this.map.removeLayer('geofence-labels');
        if (this.map.getSource('geofences-source')) this.map.removeSource('geofences-source');

        const features = [];

        geofencesList.forEach(gf => {
            if (gf.geofenceType === 1 && gf.centerLatitude && gf.centerLongitude && gf.radiusMeters) {
                // Approximate circle as a 64-sided polygon feature
                const circlePolygon = this._createGeoJsonCircle(
                    [gf.centerLongitude, gf.centerLatitude],
                    gf.radiusMeters / 1000 // in km
                );
                circlePolygon.properties = {
                    id: gf.id,
                    name: gf.name,
                    code: gf.code,
                    color: gf.colorHex || '#3b82f6',
                    type: 'circle'
                };
                features.push(circlePolygon);
            } else if (gf.geofenceType === 2 && gf.polygonGeoJson) {
                try {
                    const parsed = JSON.parse(gf.polygonGeoJson);
                    let coords;
                    if (parsed.type === 'Feature' && parsed.geometry) coords = parsed.geometry.coordinates;
                    else if (parsed.type === 'Polygon') coords = parsed.coordinates;
                    else if (Array.isArray(parsed)) coords = parsed[0] && Array.isArray(parsed[0][0]) ? parsed : [parsed];

                    if (coords) {
                        features.push({
                            type: 'Feature',
                            properties: {
                                id: gf.id,
                                name: gf.name,
                                code: gf.code,
                                color: gf.colorHex || '#3b82f6',
                                type: 'polygon'
                            },
                            geometry: {
                                type: 'Polygon',
                                coordinates: coords
                            }
                        });
                    }
                } catch (e) {
                    console.warn('Failed to parse polygon GeoJSON for geofence', gf.name, e);
                }
            }
        });

        this.map.addSource('geofences-source', {
            type: 'geojson',
            data: { type: 'FeatureCollection', features: features }
        });

        this.map.addLayer({
            id: 'geofence-polygons-fill',
            type: 'fill',
            source: 'geofences-source',
            paint: {
                'fill-color': ['get', 'color'],
                'fill-opacity': 0.18
            }
        });

        this.map.addLayer({
            id: 'geofence-polygons-line',
            type: 'line',
            source: 'geofences-source',
            paint: {
                'line-color': ['get', 'color'],
                'line-width': 2,
                'line-dasharray': [2, 2]
            }
        });

        this.map.addLayer({
            id: 'geofence-labels',
            type: 'symbol',
            source: 'geofences-source',
            layout: {
                'text-field': ['get', 'name'],
                'text-font': ['Open Sans Semibold'],
                'text-size': 12,
                'text-anchor': 'center'
            },
            paint: {
                'text-color': '#0f172a',
                'text-halo-color': '#ffffff',
                'text-halo-width': 1.5
            }
        });
    }

    /**
     * Renders a vehicle historical path / movement trail.
     */
    renderTrail(trailPoints) {
        if (!this.map || !this.isLoaded) return;

        // Cleanup previous trail
        if (this.map.getLayer('trail-line')) this.map.removeLayer('trail-line');
        if (this.map.getLayer('trail-points')) this.map.removeLayer('trail-points');
        if (this.map.getSource('trail-source')) this.map.removeSource('trail-source');

        if (!trailPoints || trailPoints.length === 0) return;

        const coordinates = trailPoints.map(p => [p.longitude, p.latitude]);

        const lineFeature = {
            type: 'Feature',
            geometry: {
                type: 'LineString',
                coordinates: coordinates
            },
            properties: {}
        };

        const pointFeatures = trailPoints.map((p, idx) => ({
            type: 'Feature',
            geometry: {
                type: 'Point',
                coordinates: [p.longitude, p.latitude]
            },
            properties: {
                index: idx,
                isStart: idx === 0,
                isEnd: idx === trailPoints.length - 1,
                time: p.recordedAtUtc,
                speed: p.speedKph || 0
            }
        }));

        this.map.addSource('trail-source', {
            type: 'geojson',
            data: {
                type: 'FeatureCollection',
                features: [lineFeature, ...pointFeatures]
            }
        });

        // Path Line
        this.map.addLayer({
            id: 'trail-line',
            type: 'line',
            source: 'trail-source',
            filter: ['==', '$type', 'LineString'],
            layout: {
                'line-join': 'round',
                'line-cap': 'round'
            },
            paint: {
                'line-color': '#3b82f6',
                'line-width': 4,
                'line-opacity': 0.85
            }
        });

        // Waypoints / Start / End circles
        this.map.addLayer({
            id: 'trail-points',
            type: 'circle',
            source: 'trail-source',
            filter: ['==', '$type', 'Point'],
            paint: {
                'circle-radius': [
                    'case',
                    ['get', 'isStart'], 8,
                    ['get', 'isEnd'], 8,
                    4
                ],
                'circle-color': [
                    'case',
                    ['get', 'isStart'], '#10b981', // green start
                    ['get', 'isEnd'], '#ef4444',   // red end
                    '#3b82f6'
                ],
                'circle-stroke-width': 2,
                'circle-stroke-color': '#ffffff'
            }
        });

        // Fit map bounds to show entire trail
        const bounds = coordinates.reduce((b, coord) => b.extend(coord), new maplibregl.LngLatBounds(coordinates[0], coordinates[0]));
        this.map.fitBounds(bounds, { padding: 60, maxZoom: 16 });
    }

    /**
     * Generates a 64-point polygon approximating a circle on earth surface.
     */
    _createGeoJsonCircle(centerLngLat, radiusKm, points = 64) {
        const coords = {
            latitude: centerLngLat[1],
            longitude: centerLngLat[0]
        };

        const km = radiusKm;
        const ret = [];
        const distanceX = km / (111.320 * Math.cos(coords.latitude * Math.PI / 180));
        const distanceY = km / 110.574;

        for (let i = 0; i < points; i++) {
            const theta = (i / points) * (2 * Math.PI);
            const x = distanceX * Math.cos(theta);
            const y = distanceY * Math.sin(theta);
            ret.push([coords.longitude + x, coords.latitude + y]);
        }
        ret.push(ret[0]); // close polygon

        return {
            type: 'Feature',
            geometry: {
                type: 'Polygon',
                coordinates: [ret]
            }
        };
    }

    resize() {
        if (this.map) {
            this.map.resize();
        }
    }
}
