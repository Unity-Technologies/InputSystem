const isOffline = !location.host || (location.host.indexOf('unity3d.com') === -1 && location.host.indexOf('unity.com') === -1);

// Purpose of this regex is to:
// 1. Check if the content is hosted under a language directory (i.e. /ja/) and assign the language (without the '/') to a variable called 'lang'
// 2. Store the package name under a variable called 'packagename'
// 3. Store the package version under a variable called 'packageversion'
// All of these stored variables will be stored under the 'pathnameMatch.groups' object
// If the package is offline or hosted locally, it will still attempt to find the package name and version
const pathRegEx = !isOffline ? /^\/(?:(?<lang>.*?)\/)?Packages\/(?<packagename>.*?)(?:@|(?:%40))(?<packageversion>.*?)\// : /\/(?<packagename>.*?)(?:@|(?:%40))(?<packageversion>.*?)\//;

const pathnameMatch = location.pathname.match(pathRegEx);
const packageName = pathnameMatch.groups.packagename;
const currentLang = pathnameMatch.groups.lang;

let hasPopulated = false;

const versionSwitcherHtml = `
<div id="version-switcher-select">
    <div class="component-select">
        <div id="component-select-current-display" class="component-select__current">
        ` + thisPackageMetaData.displayTitle + `
        </div>
        <ul id="version-switcher-ul" class="component-select__options-container">
        </ul>
    </div>
</div>
`;

function getPackageMetaData(callback){    
    let requestURL = `${!isOffline ? '/Packages' : ''}/metadata/${packageName}/metadata.json`;
    
    request(requestURL, callback);
}

function request(requestURL, callback){
    if (!hasPopulated){
        $.getJSON(requestURL, function(data){
            console.log("Getting meta data...");
            callback(data);
        }).fail(function(){
            console.log("No available meta data");
            onLastPopulate();
        });
    }
}

$(function(){
    getPackageMetaData(function(data){
        if (!data){
            onLastPopulate();
            return;
        }

        $('#breadcrumb').append($(versionSwitcherHtml)); // Create version switcher select box

        populateVersionSwitcher(data); // Populate version switcher select box (in version-switcher.js)
    });
});