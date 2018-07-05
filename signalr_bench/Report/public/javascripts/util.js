function transparentize(color, opacity) {
    var alpha = opacity === undefined ? 0.5 : 1 - opacity;
    return Color(color).alpha(alpha).rgbString();
}

function parseScenarioLabel(scenario) {
    return scenario.split("_").map(w => w.charAt(0).toUpperCase() + w.substr(1)).join(" ");
}