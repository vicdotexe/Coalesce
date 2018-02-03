import { Domain, getEnumMeta } from './coalesce/metadata' 

const domain: Domain = {}
export default domain
export const Person = domain.Person = {
  name: "person",
  type: "model",
  displayName: "Person",
  get keyProp() { return this.props.personId }, 
  get displayProp() { return this.props.name }, 
  props: {
    name: "personId",
    displayName: "Person Id",
    type: "number",
    ctor: Number,
    role: "primaryKey",
    name: "title",
    displayName: "Title",
    type: "enum",
    ...getEnumMeta([
      { value: 0, strValue: 'Mr', displayName: 'Mr' },
      { value: 1, strValue: 'Ms', displayName: 'Ms' },
      { value: 2, strValue: 'Mrs', displayName: 'Mrs' },
      { value: 4, strValue: 'Miss', displayName: 'Miss' },
    ])
    role: "value",
    name: "firstName",
    displayName: "First Name",
    type: "string",
    ctor: String,
    role: "value",
    name: "lastName",
    displayName: "Last Name",
    type: "string",
    ctor: String,
    role: "value",
    name: "email",
    displayName: "Email",
    type: "string",
    ctor: String,
    role: "value",
    name: "gender",
    displayName: "Gender",
    type: "enum",
    ...getEnumMeta([
      { value: 0, strValue: 'NonSpecified', displayName: 'NonSpecified' },
      { value: 1, strValue: 'Male', displayName: 'Male' },
      { value: 2, strValue: 'Female', displayName: 'Female' },
    ])
    role: "value",
    name: "casesAssigned",
    displayName: "Cases Assigned",
    role: "collectionNavigation",
    get model() { return Person }
    role: "value",
    name: "casesReported",
    displayName: "Cases Reported",
    role: "collectionNavigation",
    get model() { return Person }
    role: "value",
    name: "birthDate",
    displayName: "Birth Date",
    type: "date",
    ctor: "Date",
    role: "value",
    name: "lastBath",
    displayName: "Last Bath",
    type: "date",
    ctor: "Date",
    role: "value",
    name: "nextUpgrade",
    displayName: "Next Upgrade",
    type: "date",
    ctor: "Date",
    role: "value",
    name: "personStats",
    displayName: "Person Stats",
    role: "value",
    name: "name",
    displayName: "Name",
    type: "string",
    ctor: String,
    role: "value",
    name: "companyId",
    displayName: "Company Id",
    type: "number",
    ctor: Number,
    role: "foreignKey",
    name: "company",
    displayName: "Company",
    role: "referenceNavigation",
    get model() { return Person }
    role: "value",
  },
  methods: {},
}
export const Case = domain.Case = {
  name: "case",
  type: "model",
  displayName: "Case",
  get keyProp() { return this.props.caseKey }, 
  get displayProp() { return this.props.title }, 
  props: {
    name: "caseKey",
    displayName: "Case Key",
    type: "number",
    ctor: Number,
    role: "primaryKey",
    name: "title",
    displayName: "Title",
    type: "string",
    ctor: String,
    role: "value",
    name: "description",
    displayName: "Description",
    type: "string",
    ctor: String,
    role: "value",
    name: "openedAt",
    displayName: "Opened At",
    type: "date",
    ctor: "Date",
    role: "value",
    name: "assignedToId",
    displayName: "Assigned To Id",
    type: "number",
    ctor: Number,
    role: "foreignKey",
    name: "assignedTo",
    displayName: "Assigned To",
    role: "referenceNavigation",
    get model() { return Case }
    role: "value",
    name: "reportedById",
    displayName: "Reported By Id",
    type: "number",
    ctor: Number,
    role: "foreignKey",
    name: "reportedBy",
    displayName: "Reported By",
    role: "referenceNavigation",
    get model() { return Case }
    role: "value",
    name: "attachment",
    displayName: "Attachment",
    role: "value",
    name: "severity",
    displayName: "Severity",
    type: "string",
    ctor: String,
    role: "value",
    name: "status",
    displayName: "Status",
    type: "enum",
    ...getEnumMeta([
      { value: 0, strValue: 'Open', displayName: 'Open' },
      { value: 1, strValue: 'InProgress', displayName: 'InProgress' },
      { value: 2, strValue: 'Resolved', displayName: 'Resolved' },
      { value: 3, strValue: 'ClosedNoSolution', displayName: 'ClosedNoSolution' },
      { value: 4, strValue: 'Cancelled', displayName: 'Cancelled' },
    ])
    role: "value",
    name: "caseProducts",
    displayName: "Case Products",
    role: "collectionNavigation",
    get model() { return Case }
    role: "value",
    name: "devTeamAssignedId",
    displayName: "Dev Team Assigned Id",
    type: "number",
    ctor: Number,
    role: "foreignKey",
    name: "devTeamAssigned",
    displayName: "Dev Team Assigned",
    role: "referenceNavigation",
    get model() { return Case }
    role: "value",
    name: "duration",
    displayName: "Duration",
    role: "value",
  },
  methods: {},
}
export const Company = domain.Company = {
  name: "company",
  type: "model",
  displayName: "Company",
  get keyProp() { return this.props.companyId }, 
  get displayProp() { return this.props.altName }, 
  props: {
    name: "companyId",
    displayName: "Company Id",
    type: "number",
    ctor: Number,
    role: "primaryKey",
    name: "name",
    displayName: "Name",
    type: "string",
    ctor: String,
    role: "value",
    name: "address1",
    displayName: "Address1",
    type: "string",
    ctor: String,
    role: "value",
    name: "address2",
    displayName: "Address2",
    type: "string",
    ctor: String,
    role: "value",
    name: "city",
    displayName: "City",
    type: "string",
    ctor: String,
    role: "value",
    name: "state",
    displayName: "State",
    type: "string",
    ctor: String,
    role: "value",
    name: "zipCode",
    displayName: "Zip Code",
    type: "string",
    ctor: String,
    role: "value",
    name: "employees",
    displayName: "Employees",
    role: "collectionNavigation",
    get model() { return Company }
    role: "value",
    name: "altName",
    displayName: "Alt Name",
    type: "string",
    ctor: String,
    role: "value",
  },
  methods: {},
}
export const Product = domain.Product = {
  name: "product",
  type: "model",
  displayName: "Product",
  get keyProp() { return this.props.productId }, 
  get displayProp() { return this.props.name }, 
  props: {
    name: "productId",
    displayName: "Product Id",
    type: "number",
    ctor: Number,
    role: "primaryKey",
    name: "name",
    displayName: "Name",
    type: "string",
    ctor: String,
    role: "value",
  },
  methods: {},
}
export const CaseProduct = domain.CaseProduct = {
  name: "caseProduct",
  type: "model",
  displayName: "Case Product",
  get keyProp() { return this.props.caseProductId }, 
  get displayProp() { return this.props.caseProductId }, 
  props: {
    name: "caseProductId",
    displayName: "Case Product Id",
    type: "number",
    ctor: Number,
    role: "primaryKey",
    name: "caseId",
    displayName: "Case Id",
    type: "number",
    ctor: Number,
    role: "foreignKey",
    name: "case",
    displayName: "Case",
    role: "referenceNavigation",
    get model() { return CaseProduct }
    role: "value",
    name: "productId",
    displayName: "Product Id",
    type: "number",
    ctor: Number,
    role: "foreignKey",
    name: "product",
    displayName: "Product",
    role: "referenceNavigation",
    get model() { return CaseProduct }
    role: "value",
  },
  methods: {},
}
export const CaseDto = domain.CaseDto = {
  name: "caseDto",
  type: "model",
  displayName: "Case Dto",
  get keyProp() { return this.props.caseId }, 
  get displayProp() { return this.props.caseId }, 
  props: {
    name: "caseId",
    displayName: "Case Id",
    type: "number",
    ctor: Number,
    role: "primaryKey",
    name: "title",
    displayName: "Title",
    type: "string",
    ctor: String,
    role: "value",
    name: "assignedToName",
    displayName: "Assigned To Name",
    type: "string",
    ctor: String,
    role: "value",
  },
  methods: {},
}
