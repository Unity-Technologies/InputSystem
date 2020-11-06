const packagePathRegex = /.*?(?:@|(?:%40)).*?\/(?<path>.*)/;
const pagePath = location.pathname.match(packagePathRegex).groups.path;

let versionsAdded = 0;
let versionsToAdd = [];

function populateVersionSwitcher(metadata){
    let versionsInfo = metadata.versions;
    let versionsToPopulate = {};
    let numberToPopulate = 0;
    let populatedCounter = 0;

    for (let version in versionsInfo){
        let versionSplit = version.split('.');
        let versionTrimmed = versionSplit[0] + '.' + versionSplit[1];
        let currentVersionSplit = thisPackageMetaData.version.split('.');
        let currentVersionTrimmed = currentVersionSplit[0] + '.' + currentVersionSplit[1];
        let unityVersion = "";
        if(versionsInfo[version].unity)
            unityVersion += versionsInfo[version].unity;
        if(versionsInfo[version].unityRelease)
            unityVersion += "." + versionsInfo[version].unityRelease;
        
        if (!versionsToPopulate[versionTrimmed] && versionTrimmed != currentVersionTrimmed){
            versionsToPopulate[versionTrimmed] = {versionTrimmed: versionTrimmed, unityVersion:unityVersion};
            numberToPopulate++;
        }
    }

    let versionsToPopulateLength = 0;

    for (let versionTrimmed in versionsToPopulate){
        versionsToPopulateLength++;
        // Ajax call, if success, add, else use fallback
        (function(version){
            let gotoUrl = getRedirectUrl(version);

            $.ajax( gotoUrl )
            .done(function() {
                addToList(version, gotoUrl, versionsToPopulate[versionTrimmed].unityVersion);
                versionsAdded++;
                if (++populatedCounter >= numberToPopulate){
                    onLastPopulate();
                }
            })
            .fail(function() {
                let indexUrl = getFallbackRedirectUrl(version);
                addToList(version, indexUrl, versionsToPopulate[versionTrimmed].unityVersion);
                versionsAdded++;

                if (++populatedCounter >= numberToPopulate){
                    onLastPopulate();
                }
            });        
        })(versionTrimmed);
    }

    

    if (versionsToPopulateLength <= 0){
        onLastPopulate();
    }
}

function onLastPopulate(){
    if (versionsAdded <= 0){
        $('#version-switcher-select').remove();
        $('#breadcrumb').append($('<p style="margin: 10px 0;"><b>' + thisPackageMetaData.displayTitle + '</b></p>'));
    }
    else {
        versionsToAdd = versionsToAdd.sort( 
            (a, b) => -a.version.localeCompare(b.version, "en-US", { numeric:true }) 
        );

        for (var i = 0; i < versionsToAdd.length; i++){
            createVersionOption(versionsToAdd[i].version, versionsToAdd[i].gotoUrl, versionsToAdd[i].unityVersion);
        }

        onVersionSwitcherClick();
    }
}

function addToList(version, gotoUrl, unityVersion){
    versionsToAdd.push({version:version, gotoUrl:gotoUrl, unityVersion:unityVersion});
    ++versionsAdded;
}

function createVersionOption(version, gotoUrl, unityVersion){
    let item ="";
    if(unityVersion)
        item = $(`<a style="color:#000;" href="${gotoUrl}"><li class="component-select__option" style='justify-content:space-between;'>${version} <span style="color:#aaa;">${unityVersion}+</span></li></a>`);
    else
        item = $(`<a style="color:#000;" href="${gotoUrl}"><li class="component-select__option">${version}</li></a>`);
    $('#version-switcher-ul').append(item);
}

function getRedirectUrl(versionTrimmed){
    let output = `/Packages/${packageName}@${versionTrimmed}/`;
    if (currentLang && currentLang !== 'en')
        output = `/${currentLang}${output}`;
    output += `${pagePath}`;
    return output;
}

function getFallbackRedirectUrl(versionTrimmed) {
    let output = `/Packages/${packageName}@${versionTrimmed}/`;
    if (currentLang && currentLang !== 'en')
        output = `/${currentLang}${output}`;
    return output;
}

function onVersionSwitcherClick(){
    $('#component-select-current-display').click(function(){
        $('#component-select-current-display').toggleClass('component-select__current--is-active');
    });
}

$(document).click(function(e){
    if (!(e.target.id == 'component-select-current-display'))
        $('#component-select-current-display').removeClass('component-select__current--is-active');
});