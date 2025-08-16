import './style.css'

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY;
const mapId = import.meta.env.VITE_GOOGLE_MAPS_MAP_ID;

function loadGoogleMaps(apiKey, callback) {
  const existingScript = document.getElementById('googleMaps');
  if (!existingScript) {
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&callback=${callback}&libraries=maps,marker`;
    script.id = 'googleMaps';
    script.async = true;
    script.defer = true;
    script.setAttribute("loading", "async");
    document.body.appendChild(script);
    console.log(apiKey, mapId);
  }
}

// Building dataset (hardcoded some buildings in Bratislava, Slovakia)
const buildings = [
  {
    name: "Main Building - Bratislava Castle",
    position: { lat: 48.1421, lng: 17.1000 },
    type: "Main",
    color: "red"
  },
  {
    name: "Office 1 - Eurovea Tower",
    position: { lat: 48.139802, lng: 17.1253951 },
    type: "Office",
    color: "blue"
  },
  {
    name: "Office 2 - Nivy Tower",
    position: { lat: 48.1463764, lng: 17.1275149 },
    type: "Office",
    color: "blue"
  },
  {
    name: "Healthcare Center - Kramáre Hospital",
    position: { lat: 48.1674101, lng: 17.0862015 },
    type: "Healthcare",
    color: "blue"
  },
  {
    name: "Industrial Plant - Refinery Slovnaft",
    position: { lat: 48.1281979, lng: 17.1717301 },
    type: "Industrial",
    color: "blue"
  }
];

let map;
let markers = [];

window.initMap = function () {
  console.log("Google Maps are being initialized");
  map = new google.maps.Map(document.getElementById("map"), {
    center: { lat: 48.1450, lng: 17.1100 },
    zoom: 14,
    mapId: mapId,
  });

  const { AdvancedMarkerElement } = google.maps.marker;
  console.log(`AdvancedMarkerElement is available: ${AdvancedMarkerElement}`);

  buildings.forEach(building => {
    const marker = new AdvancedMarkerElement({
      position: building.position,
      map,
      title: building.name,
    });

    marker.buildingType = building.type;
    markers.push(marker);
  });

  const controlDiv = document.createElement("div");
  const select = document.createElement("select");

  const allOption = document.createElement("option");
  allOption.value = "All";
  allOption.text = "Show All";
  select.appendChild(allOption);

  const types = [...new Set(buildings.map(b => b.type))];
  types.forEach(type => {
    const option = document.createElement("option");
    option.value = type;
    option.text = type;
    select.appendChild(option);
  });

  select.style.margin = "10px";
  select.classList.add("gm-control-active");
  select.addEventListener("change", () => {
    console.log(`Filter changed to: ${select.value}`);
    const value = select.value;
    markers.forEach((marker, i) => {
      if (value === "All" || buildings[i].type === value || buildings[i].type === "Main") {
        marker.setMap(map);
      } else {
        marker.setMap(null);
      }
    });
  });

  controlDiv.appendChild(select);
  map.controls[google.maps.ControlPosition.TOP_RIGHT].push(controlDiv);
};

window.addEventListener("DOMContentLoaded", () => {
  console.log("DOM fully loaded and parsed");
  const appDiv = document.querySelector('#app');
  if (appDiv) {
    appDiv.innerHTML = `<div id="map" class="app2"></div>`;
    window.initMap = window.initMap || function () { };
    loadGoogleMaps(apiKey, "initMap");
  } else {
    console.error("Element with id 'app' not found.");
  }
});
