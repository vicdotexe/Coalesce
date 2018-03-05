
/// <reference path="../coalesce.dependencies.d.ts" />

// Generated by IntelliTect.Coalesce

module ViewModels {
    
    export class CaseDto extends Coalesce.BaseViewModel {
        public readonly modelName = "CaseDto";
        public readonly primaryKeyName: keyof this = "caseId";
        public readonly modelDisplayName = "Case Dto";
        public readonly apiController = "/CaseDto";
        public readonly viewController = "/CaseDto";
        
        /** Behavioral configuration for all instances of CaseDto. Can be overidden on each instance via instance.coalesceConfig. */
        public static coalesceConfig: Coalesce.ViewModelConfiguration<CaseDto>
            = new Coalesce.ViewModelConfiguration<CaseDto>(Coalesce.GlobalConfiguration.viewModel);
        
        /** Behavioral configuration for the current CaseDto instance. */
        public coalesceConfig: Coalesce.ViewModelConfiguration<this>
            = new Coalesce.ViewModelConfiguration<CaseDto>(CaseDto.coalesceConfig);
        
        /** The namespace containing all possible values of this.dataSource. */
        public dataSources: typeof ListViewModels.CaseDtoDataSources = ListViewModels.CaseDtoDataSources;
        
        
        public caseId: KnockoutObservable<number | null> = ko.observable(null);
        public title: KnockoutObservable<string | null> = ko.observable(null);
        public assignedToName: KnockoutObservable<string | null> = ko.observable(null);
        
        
        
        
        
        
        
        /** 
            Load the ViewModel object from the DTO.
            @param force: Will override the check against isLoading that is done to prevent recursion. False is default.
            @param allowCollectionDeletes: Set true when entire collections are loaded. True is the default. 
            In some cases only a partial collection is returned, set to false to only add/update collections.
        */
        public loadFromDto = (data: any, force: boolean = false, allowCollectionDeletes: boolean = true): void => {
            if (!data || (!force && this.isLoading())) return;
            this.isLoading(true);
            // Set the ID 
            this.myId = data.caseId;
            this.caseId(data.caseId);
            // Load the lists of other objects
            
            // The rest of the objects are loaded now.
            this.title(data.title);
            this.assignedToName(data.assignedToName);
            if (this.coalesceConfig.onLoadFromDto()){
                this.coalesceConfig.onLoadFromDto()(this as any);
            }
            this.isLoading(false);
            this.isDirty(false);
            if (this.coalesceConfig.validateOnLoadFromDto()) this.validate();
        };
        
        /** Saves this object into a data transfer object to send to the server. */
        public saveToDto = (): any => {
            var dto: any = {};
            dto.caseId = this.caseId();
            
            dto.title = this.title();
            
            return dto;
        }
        
        /** 
            Loads any child objects that have an ID set, but not the full object.
            This is useful when creating an object that has a parent object and the ID is set on the new child.
        */
        public loadChildren = (callback?: () => void): void => {
            var loadingCount = 0;
            if (loadingCount == 0 && typeof(callback) == "function") { callback(); }
        };
        
        public setupValidation(): void {
            if (this.errors !== null) return;
            this.errors = ko.validation.group([
            ]);
            this.warnings = ko.validation.group([
            ]);
        }
        
        constructor(newItem?: object, parent?: Coalesce.BaseViewModel | ListViewModels.CaseDtoList) {
            super(parent);
            this.baseInitialize();
            const self = this;
            
            
            
            
            
            
            self.title.subscribe(self.autoSave);
            
            if (newItem) {
                self.loadFromDto(newItem, true);
            }
        }
    }
    
    export namespace CaseDto {
    }
}
