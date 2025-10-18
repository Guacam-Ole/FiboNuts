window.blazorCulture = {
    loadSatelliteAssembly: async function (culture) {
        try {
            console.log(`Attempting to load satellite assembly for culture: ${culture}`);
            
            // For Blazor WebAssembly, we need to load the satellite assembly manually
            const assemblyName = `${culture}/BalatroPoker.resources.dll`;
            
            // Try to load the assembly via Blazor's module loading
            if (window.Blazor && window.Blazor.platform) {
                const url = `_framework/${assemblyName}`;
                console.log(`Loading satellite assembly from: ${url}`);
                
                // Force load the satellite assembly
                await window.Blazor.platform.loadSatelliteAssembly(assemblyName);
                console.log(`Successfully loaded satellite assembly: ${assemblyName}`);
                return true;
            } else {
                console.warn('Blazor platform not available for satellite assembly loading');
                return false;
            }
        } catch (error) {
            console.error(`Failed to load satellite assembly for ${culture}:`, error);
            return false;
        }
    },
    
    preloadSatelliteAssemblies: async function () {
        const cultures = ['de', 'fr', 'it', 'es'];
        
        for (const culture of cultures) {
            try {
                await this.loadSatelliteAssembly(culture);
            } catch (error) {
                console.warn(`Could not preload satellite assembly for ${culture}:`, error);
            }
        }
    }
};

// Preload satellite assemblies when Blazor starts
document.addEventListener('DOMContentLoaded', async function () {
    if (window.Blazor) {
        await window.blazorCulture.preloadSatelliteAssemblies();
    } else {
        // Wait for Blazor to load
        const checkBlazor = setInterval(async () => {
            if (window.Blazor && window.Blazor.platform) {
                clearInterval(checkBlazor);
                await window.blazorCulture.preloadSatelliteAssemblies();
            }
        }, 100);
    }
});